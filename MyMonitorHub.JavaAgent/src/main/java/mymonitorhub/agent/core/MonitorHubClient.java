package mymonitorhub.agent.core;

import com.fasterxml.jackson.databind.ObjectMapper;
import mymonitorhub.agent.common.MonitorProfile;
import org.java_websocket.client.WebSocketClient;
import org.java_websocket.handshake.ServerHandshake;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.net.ssl.SSLContext;
import javax.net.ssl.TrustManager;
import javax.net.ssl.X509TrustManager;
import ms.VMS;
import ms.VMSProcess;
import ms.VMSProcessList;
import mymonitorhub.agent.common.MonitorProfile.ServiceDefinition;

import java.net.URI;
import java.security.cert.X509Certificate;
import java.util.HashMap;
import java.util.Map;

/**
 * Connects to the hub's WebSocket endpoint, handles pipe-delimited commands,
 * and auto-reconnects on close or error.
 * Mirrors C# MonitorHubClient + ManagedWebSocket.
 */
public class MonitorHubClient {

    private static final Logger log = LoggerFactory.getLogger(MonitorHubClient.class);
    private static final ObjectMapper MAPPER = new ObjectMapper();

    private final int accountId;
    private final int deviceId;
    private volatile boolean stopped = false;
    private HubWebSocket ws;

    public MonitorHubClient(int accountId, int deviceId) {
        this.accountId = accountId;
        this.deviceId  = deviceId;
    }

    public void start() {
        try {
            log.info("MonitorHubClient: Start");
            String accessToken = MonitorProfile.getTokenManager().getAccessToken();
            MonitorProfile profile = MonitorProfile.getCurrent();

            Map<String, String> headers = new HashMap<String, String>();
            headers.put("Authorization", "Bearer " + accessToken);

            URI uri = new URI(profile.getWebSocketAddress() + "/api/Hub/GetForAgent");
            ws = new HubWebSocket(uri, headers, profile.isSslTrustAll(), this);
            ws.connect();
        } catch (Exception ex) {
            log.error("MonitorHubClient start error: {}", ex.getMessage());
            if (!stopped) {
                scheduleReconnect();
            }
        }
    }

    public void stop() {
        stopped = true;
        log.info("MonitorHubClient: Stop");
        if (ws != null) {
            try { ws.close(); } catch (Exception ignored) {}
        }
    }

    void onOpen() {
        log.info("MonitorHubClient: Connected.");
    }

    void onClose() {
        if (stopped) {
            log.info("MonitorHubClient: Stopped");
            return;
        }
        log.info("MonitorHubClient: Closed — reconnecting");
        try { Thread.sleep(10000); } catch (InterruptedException ignored) {}
        start();
    }

    void onError(Exception ex) {
        log.error("MonitorHubClient error: {}", ex.getMessage());
    }

    void onMessage(String message) {
        if (message.startsWith("Registered: ")) {
            String connectionId = message.substring("Registered: ".length());
            log.info("Registered with hub. ConnectionId: {}", connectionId);
            return;
        }

        int sep = message.indexOf('|');
        if (sep == -1) {
            log.warn("Received WebSocket message with unexpected format (no '|' separator); message ignored.");
            return;
        }

        Puller puller = new Puller(message);
        String from = puller.pull();
        if ("server".equals(from)) {
            log.debug("Received from {}", from);
            return;
        }

        String command = puller.pull();
        log.debug("Received from: {} - command: {}", from, command);

        try {
            switch (command) {
                case "service": {
                    String codeId = puller.pull();
                    String serviceName = puller.pull();
                    String action = puller.pull();
                    onService(command, from, codeId, serviceName, action);
                    break;
                }
                case "services": {
                    String code = puller.pull();
                    onServices(command, from, code);
                    break;
                }
                case "topCpu": {
                    String takeStr = puller.pull();
                    int take;
                    try {
                        take = Integer.parseInt(takeStr);
                    } catch (NumberFormatException e) {
                        sendFailure(from, command, "Invalid take value.");
                        break;
                    }
                    if (take < 1 || take > 50) {
                        sendFailure(from, command, "Invalid take value.");
                        break;
                    }
                    onTopCpu(command, from, take);
                    break;
                }
                case "sysInfo":
                    onSysInfo(command, from);
                    break;
                default:
                    sendFailure(from, command, "Unknown Command");
            }
        } catch (Exception e) {
            log.error("onMessage handler error: {}", e.getMessage());
            sendFailure(from, command, e.getMessage());
        }
    }

    // ── Command handlers ───────────────────────────────────────────────────────

    private void onService(String command, String toGuid, String code,
                           String serviceName, String action) {
        ServiceDefinition def = findServiceDef(serviceName);
        if (def == null) {
            log.warn("onService> Rejected — '{}' is not in the configured service list.", serviceName);
            sendFailure(toGuid, command,
                    "Service '" + serviceName + "' is not in the configured service list.");
            return;
        }
        log.debug("onService> action={} service={}", action, serviceName);
        try {
            if ("start".equals(action)) {
                String vmsResult = VMS.Requester("|START_SERVICE|" + serviceName + "|" + def.startCommand);
                log.debug("onService> VMS START_SERVICE returned: {}", vmsResult);
            } else if ("stop".equals(action)) {
                Integer pid = findPidByName(serviceName);
                if (pid != null) {
                    String vmsResult = VMS.Requester("|STOP_SERVICE_BY_PID|" + pid);
                    log.debug("onService> VMS STOP_SERVICE_BY_PID({}) returned: {}", pid, vmsResult);
                } else {
                    log.debug("onService> '{}' not found in process list — already stopped", serviceName);
                }
            } else {
                log.warn("onService> Unknown action '{}' for service '{}'", action, serviceName);
                sendFailure(toGuid, command, "Unknown service action '" + action + "'.");
                return;
            }

            // Poll up to 20 seconds for the service to reach the expected state,
            // mirroring C# ServiceController.WaitForStatus(timeout=20s).
            boolean expectedRunning = "start".equals(action);
            boolean isRunning = VMS.processExists(serviceName);
            for (int i = 0; i < 20 && isRunning != expectedRunning; i++) {
                log.debug("onService> waiting for '{}' to {}: isRunning={} (attempt {})",
                        serviceName, action, isRunning, i + 1);
                Thread.sleep(1000);
                isRunning = VMS.processExists(serviceName);
            }

            String statusDesc = isRunning ? "Running" : "Stopped";
            log.debug("onService> final status for '{}': {}", serviceName, statusDesc);
            Map<String, Object> resp = new HashMap<String, Object>();
            resp.put("Code", code);
            resp.put("Checked", isRunning);
            resp.put("StatusDesc", statusDesc);
            sendSuccess(toGuid, command, resp);
        } catch (Exception e) {
            log.error("onService> error for '{}': {}", serviceName, e.getMessage());
            sendFailure(toGuid, command, e.getMessage());
        }
    }

    private void onServices(String command, String toGuid, String code) {
        try {
            ServiceDefinition[] defs = MonitorProfile.getCurrent().getServiceDefinitions();
            java.util.List<Map<String, Object>> list =
                    new java.util.ArrayList<Map<String, Object>>();
            for (ServiceDefinition def : defs) {
                boolean running = VMS.processExists(def.name);
                Map<String, Object> entry = new HashMap<String, Object>();
                entry.put("Code",        def.name);
                entry.put("Name",        def.name);
                entry.put("Description", def.description);
                entry.put("Checked",     running);
                entry.put("StatusDesc",  running ? "Running" : "Stopped");
                list.add(entry);
            }
            sendSuccess(toGuid, command, list);
        } catch (Exception e) {
            log.error(e.getMessage());
            sendFailure(toGuid, command, e.getMessage());
        }
    }

    private void onTopCpu(String command, String toGuid, int take) {
        try {
            log.debug("received topCpu request");
            ProcessCollectionThread collector = Monitor.getProcessCollector();
            if (collector == null) {
                sendFailure(toGuid, command, "Process collector not available.");
                return;
            }
            String result = collector.topCpu(take);
            // result is already JSON; wrap it to match C# sendSuccess shape
            sendRaw(toGuid, command, result);
            log.debug("Sent TopCpuResponse");
        } catch (Exception e) {
            log.error(e.getMessage());
            sendFailure(toGuid, command, e.getMessage());
        }
    }

    private void onSysInfo(String command, String toGuid) {
        try {
            log.debug("received sysInfo request");
            String result = new CollectSysInfo().collect();
            sendRaw(toGuid, command, result);
            log.debug("Sent SysInfoResponse");
        } catch (Exception e) {
            log.error(e.getMessage());
            sendFailure(toGuid, command, e.getMessage());
        }
    }

    // ── Send helpers ───────────────────────────────────────────────────────────

    private void send(String toGuid, String json) {
        if (ws != null) {
            try {
                ws.send("Request|" + toGuid + "|" + json);
            } catch (Exception ex) {
                log.error("ws send error: {}", ex.getMessage());
            }
        }
    }

    private void sendSuccess(String toGuid, String command, Object data) {
        try {
            Map<String, Object> obj = new HashMap<String, Object>();
            obj.put("Success", true);
            obj.put("Command", command);
            obj.put("Data", data);
            send(toGuid, MAPPER.writeValueAsString(obj));
        } catch (Exception e) {
            log.error("sendSuccess error: {}", e.getMessage());
        }
    }

    /** Sends an already-serialised JSON string as the Data field. */
    private void sendRaw(String toGuid, String command, String dataJson) {
        String json = "{\"Success\":true,\"Command\":\"" + command + "\",\"Data\":" + dataJson + "}";
        send(toGuid, json);
    }

    private void sendFailure(String toGuid, String command, String reason) {
        try {
            log.error("SendFailure> {}, command = {}", toGuid, command);
            Map<String, Object> obj = new HashMap<String, Object>();
            obj.put("Success", false);
            obj.put("Command", command);
            obj.put("FailureReason", reason);
            send(toGuid, MAPPER.writeValueAsString(obj));
        } catch (Exception e) {
            log.error("sendFailure error: {}", e.getMessage());
        }
    }

    // ── Service helpers ────────────────────────────────────────────────────────

    /**
     * Scans the live process table and returns the PID of the first process
     * whose name matches serviceName (case-insensitive), or null if not found.
     */
    private Integer findPidByName(String serviceName) {
        VMSProcessList list = new VMSProcessList();
        VMS.getPidAndCpu(list);
        for (VMSProcess p : list.getList().values()) {
            String name = VMS.getProcessName(p.pid);
            if (serviceName.equalsIgnoreCase(name)) {
                return p.pid;
            }
        }
        return null;
    }

    private ServiceDefinition findServiceDef(String serviceName) {
        for (ServiceDefinition def : MonitorProfile.getCurrent().getServiceDefinitions()) {
            if (def.name.equals(serviceName)) return def;
        }
        return null;
    }

    private void scheduleReconnect() {
        Thread t = new Thread(new Runnable() {
            @Override
            public void run() {
                try { Thread.sleep(10000); } catch (InterruptedException ignored) {}
                start();
            }
        });
        t.setDaemon(true);
        t.start();
    }

    // ── Puller (pipe-delimited token extractor) ────────────────────────────────

    static class Puller {
        private final String text;
        private int lastPos = -1;

        Puller(String text) { this.text = text; }

        String pull() {
            if (lastPos + 1 >= text.length()) return "";
            int idx = text.indexOf('|', lastPos + 1);
            if (idx == -1) {
                String result = text.substring(lastPos + 1);
                lastPos = text.length() - 1;
                return result;
            }
            String result = text.substring(lastPos + 1, idx);
            lastPos = idx;
            return result;
        }

        String tail() {
            if (lastPos + 1 >= text.length()) return "";
            return text.substring(lastPos + 1);
        }
    }

    // ── Inner WebSocket implementation ─────────────────────────────────────────

    private static class HubWebSocket extends WebSocketClient {

        private final MonitorHubClient owner;

        HubWebSocket(URI uri, Map<String, String> headers,
                     boolean trustAll, MonitorHubClient owner) throws Exception {
            super(uri, headers);
            this.owner = owner;
            if (uri.getScheme().equalsIgnoreCase("wss")) {
                SSLContext sslContext;
                if (trustAll) {
                    sslContext = buildTrustAllContext();
                } else {
                    sslContext = SSLContext.getInstance("TLS");
                    sslContext.init(null, null, null);
                }
                setSocketFactory(sslContext.getSocketFactory());
            }
        }

        @Override
        public void onOpen(ServerHandshake handshake) {
            owner.onOpen();
        }

        @Override
        public void onMessage(String message) {
            owner.onMessage(message);
        }

        @Override
        public void onClose(int code, String reason, boolean remote) {
            log.info("WebSocket closed: {} {}", code, reason);
            owner.onClose();
        }

        @Override
        public void onError(Exception ex) {
            owner.onError(ex);
        }

        private static SSLContext buildTrustAllContext() throws Exception {
            SSLContext ctx = SSLContext.getInstance("TLS");
            ctx.init(null, new TrustManager[]{new X509TrustManager() {
                @Override
                public void checkClientTrusted(X509Certificate[] chain, String authType) {}
                @Override
                public void checkServerTrusted(X509Certificate[] chain, String authType) {}
                @Override
                public X509Certificate[] getAcceptedIssuers() { return new X509Certificate[0]; }
            }}, null);
            return ctx;
        }
    }
}
