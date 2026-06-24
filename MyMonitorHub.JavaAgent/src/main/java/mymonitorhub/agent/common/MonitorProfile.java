package mymonitorhub.agent.common;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.io.IOException;
import java.nio.file.*;

/**
 * Singleton configuration model — mirrors C# MonitorProfile.
 *
 * Config uses plain-text "api-key" and "refresh-token" because DPAPI is
 * unavailable in Java. The C# "api-key-protected" / "refresh-token-protected"
 * fields are ignored.
 *
 * Set environment variable MYMONITORHUB_JAVAAGENT_CONFIG_DIR to override the
 * directory where appsettings.json is read from.
 */
public class MonitorProfile {

    private static final Logger log = LoggerFactory.getLogger(MonitorProfile.class);
    private static final ObjectMapper MAPPER = new ObjectMapper();
    public static final String MONITOR_FILE_NAME = "appsettings.json";
    public static final String TOKEN_FILE_NAME = "refresh-token.dat";

    /** Shared lock that guards all writes to appsettings.json. */
    public static final Object FILE_WRITE_LOCK = new Object();

    private static MonitorProfile current;
    private static AgentTokenManager tokenManager;
    private static final Object lock = new Object();
    private static WatchService watchService;
    private static boolean internalWrite = false;

    // ── Fields ─────────────────────────────────────────────────────────────────

    private int accountId;
    private int deviceId;
    private String apiKey;
    private String refreshToken;
    private int commandPort;
    private int reconnectAttemptMinutes;
    private String accountName;
    private String groupName;
    private String deviceName;
    private String baseHost;   // host[:port] without scheme
    private boolean sslTrustAll;
    private ServiceDefinition[] serviceDefinitions;

    private JsonNode rootConfig;

    // ── ServiceDefinition ──────────────────────────────────────────────────────

    public static class ServiceDefinition {
        public final String name;
        public final String description;
        public final String startCommand;

        public ServiceDefinition(String name, String description, String startCommand) {
            this.name         = name;
            this.description  = description;
            this.startCommand = startCommand;
        }
    }

    // ── Static accessors ───────────────────────────────────────────────────────

    /**
     * Resolves the config directory in priority order:
     * 1. MYMONITORHUB_JAVAAGENT_CONFIG_DIR environment variable (explicit override)
     * 2. ../conf relative to the current working directory
     */
    public static String resolveConfigDir() {
        String envOverride = System.getenv("MYMONITORHUB_JAVAAGENT_CONFIG_DIR");
        if (envOverride != null && !envOverride.trim().isEmpty()) {
            return envOverride;
        }

        try {
            return new File(System.getProperty("user.dir"), "../conf").getCanonicalPath();
        } catch (IOException e) {
            log.warn("Could not resolve ../conf from working directory: {}", e.getMessage());
            return System.getProperty("user.dir");
        }
    }

    public static String getMonitorFilePath() {
        return resolveConfigDir() + File.separator + MONITOR_FILE_NAME;
    }

    public static String getTokenFilePath() {
        return resolveConfigDir() + File.separator + TOKEN_FILE_NAME;
    }

    public static AgentTokenManager getTokenManager() {
        synchronized (lock) {
            if (tokenManager == null) {
                tokenManager = new AgentTokenManager();
            }
            return tokenManager;
        }
    }

    public static MonitorProfile getCurrent() {
        synchronized (lock) {
            if (current == null) {
                current = new MonitorProfile();
                if (watchService == null) {
                    startFileWatcher();
                }
            }
            return current;
        }
    }

    /** Forces a reload on the next getCurrent() call. Ignored for internal writes. */
    public static void invalidate() {
        synchronized (lock) {
            if (internalWrite) {
                internalWrite = false;
                return;
            }
            current = null;
            log.info(MONITOR_FILE_NAME + " change detected — will reload on next access.");
        }
    }

    // ── Constructor ────────────────────────────────────────────────────────────

    private MonitorProfile() {
        load();
    }

    private void load() {
        String path = getMonitorFilePath();
        try {
            rootConfig = MAPPER.readTree(new File(path));
        } catch (IOException e) {
            throw new RuntimeException("Failed to read " + path, e);
        }

        JsonNode server = rootConfig.path("server");

        accountId  = server.path("account-id").asInt(0);
        deviceId   = server.path("device-id").asInt(0);
        apiKey     = server.path("api-key").asText(null);

        // Refresh token lives in its own file so a crash during write can never corrupt appsettings.json.
        File tokenFile = new File(getTokenFilePath());
        if (tokenFile.exists()) {
            try {
                String stored = new String(Files.readAllBytes(tokenFile.toPath())).trim();
                refreshToken = stored.isEmpty() ? null : stored;
            } catch (IOException e) {
                log.warn("Could not read {}: {}", TOKEN_FILE_NAME, e.getMessage());
                refreshToken = null;
            }
        } else {
            // One-time migration: if the legacy appsettings.json field is present, move it to the token file.
            String legacy = server.path("refresh-token").asText(null);
            if (legacy != null && !legacy.isEmpty()) {
                refreshToken = legacy;
                log.info("MonitorProfile: migrating refresh token from appsettings.json to {}.", TOKEN_FILE_NAME);
                writeTokenFile(legacy);
            } else {
                refreshToken = null;
            }
        }
        commandPort = server.path("command-port").asInt(6800);
        reconnectAttemptMinutes = server.path("reconnect-attempt-minutes").asInt(45);
        accountName = server.path("account-name").asText(null);
        groupName   = server.path("group-name").asText(null);
        deviceName  = server.path("device-name").asText(null);
        sslTrustAll = server.path("ssl-trust-all").asBoolean(false);

        String url = server.path("url").asText("");
        int idx = url.indexOf("//");
        baseHost = (idx >= 0) ? url.substring(idx + 2) : url;
        // Strip trailing slash
        if (baseHost.endsWith("/")) {
            baseHost = baseHost.substring(0, baseHost.length() - 1);
        }

        JsonNode servicesNode = server.path("services");
        if (servicesNode.isArray()) {
            serviceDefinitions = new ServiceDefinition[servicesNode.size()];
            for (int i = 0; i < servicesNode.size(); i++) {
                JsonNode svc = servicesNode.get(i);
                serviceDefinitions[i] = new ServiceDefinition(
                        svc.path("name").asText(""),
                        svc.path("description").asText(""),
                        svc.path("start-command").asText("")
                );
            }
        } else {
            serviceDefinitions = new ServiceDefinition[0];
        }
    }

    /**
     * Persists the current refresh token to its dedicated file using an atomic
     * write (temp file + rename) so a crash mid-write can never produce an empty file.
     */
    public void writeRefreshToken() {
        if (refreshToken == null || refreshToken.isEmpty()) return;
        writeTokenFile(refreshToken);
    }

    /**
     * Atomically writes {@code token} to {@link #TOKEN_FILE_NAME} in the config directory.
     * Writes to a .tmp file first then renames so the target is never left empty on crash.
     */
    private static void writeTokenFile(String token) {
        Path target = Paths.get(getTokenFilePath());
        Path tmp    = Paths.get(getTokenFilePath() + ".tmp");
        try {
            Files.write(tmp, token.getBytes(java.nio.charset.StandardCharsets.UTF_8));
            Files.move(tmp, target, StandardCopyOption.REPLACE_EXISTING, StandardCopyOption.ATOMIC_MOVE);
        } catch (IOException e) {
            log.error("Failed to write {} : {}", TOKEN_FILE_NAME, e.getMessage());
        }
    }

    // ── File watcher ───────────────────────────────────────────────────────────

    private static void startFileWatcher() {
        try {
            Path watchPath = Paths.get(resolveConfigDir());
            watchService = FileSystems.getDefault().newWatchService();
            watchPath.register(watchService, StandardWatchEventKinds.ENTRY_MODIFY);

            Thread watchThread = new Thread(new Runnable() {
                @Override
                public void run() {
                    try {
                        while (true) {
                            WatchKey key = watchService.take();
                            for (WatchEvent<?> event : key.pollEvents()) {
                                Object ctx = event.context();
                                if (ctx != null && ctx.toString().equals(MONITOR_FILE_NAME)) {
                                    invalidate();
                                }
                            }
                            if (!key.reset()) break;
                        }
                    } catch (InterruptedException | ClosedWatchServiceException e) {
                        // normal shutdown
                    }
                }
            });
            watchThread.setDaemon(true);
            watchThread.setName("config-watcher");
            watchThread.start();
        } catch (IOException e) {
            log.warn("Could not start config file watcher: {}", e.getMessage());
        }
    }

    // ── Getters ────────────────────────────────────────────────────────────────

    public int getAccountId()    { return accountId; }
    public int getDeviceId()     { return deviceId; }
    public String getApiKey()    { return apiKey; }

    public String getRefreshToken() { return refreshToken; }
    public void setRefreshToken(String token) { this.refreshToken = token; }

    public int getCommandPort()  { return commandPort; }
    public int getReconnectAttemptMinutes() { return reconnectAttemptMinutes; }
    public String getAccountName() { return accountName; }
    public String getGroupName()   { return groupName; }
    public String getDeviceName()  { return deviceName; }
    public boolean isSslTrustAll() { return sslTrustAll; }

    public ServiceDefinition[] getServiceDefinitions() { return serviceDefinitions; }

    public String[] getServiceNames() {
        String[] names = new String[serviceDefinitions.length];
        for (int i = 0; i < serviceDefinitions.length; i++) {
            names[i] = serviceDefinitions[i].name;
        }
        return names;
    }

    /** https://host[:port]/ */
    public String getBaseAddress() {
        return "https://" + baseHost + "/";
    }

    /** wss://host[:port] */
    public String getWebSocketAddress() {
        return "wss://" + baseHost;
    }

    public JsonNode getRootConfig() {
        return rootConfig;
    }
}
