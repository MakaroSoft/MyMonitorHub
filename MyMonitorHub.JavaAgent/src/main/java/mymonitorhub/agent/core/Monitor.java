package mymonitorhub.agent.core;

import com.fasterxml.jackson.databind.JsonNode;
import mymonitorhub.agent.common.MonitorProfile;
import mymonitorhub.agent.model.Event;
import mymonitorhub.agent.model.EventType;
import mymonitorhub.agent.model.ItemStyle;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
import java.util.Date;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Main orchestrator — starts all background threads and hosts the event dedup map.
 * Mirrors C# Monitor.
 */
public class Monitor {

    private static final Logger log = LoggerFactory.getLogger(Monitor.class);

    private static int port;
    private static final EventQueue EVENT_QUEUE = new EventQueue();
    private static final Map<String, ItemData> items = new HashMap<String, ItemData>();
    private static final Object itemsLock = new Object();

    /** Lock used to guard file writes (mainly config) across threads. */
    public static final Object FILE_WRITE_LOCK = MonitorProfile.FILE_WRITE_LOCK;

    private static ProcessCollectionThread processCollector;
    private MonitorHubClient monitorHubClient;
    private final List<IThreadShutdown> threads = new ArrayList<IThreadShutdown>();

    public static int getPort() { return port; }

    static List<Event> getEventList() { return EVENT_QUEUE.drain(); }

    public static ProcessCollectionThread getProcessCollector() { return processCollector; }

    // ── Startup ────────────────────────────────────────────────────────────────

    public void run() {
        try {
            runTrapped();
        } catch (Exception e) {
            log.error("{}\n{}", e.getMessage(), stackTrace(e));
        }
    }

    private void runTrapped() throws Exception {
        MonitorProfile profile = MonitorProfile.getCurrent();

        monitorHubClient = new MonitorHubClient(profile.getAccountId(), profile.getDeviceId());
        monitorHubClient.start();

        port = profile.getCommandPort();

        JsonNode config = readConfig();
        if (!config.has("version")) {
            throw new RuntimeException("Error in appsettings.json, expecting 'version' field");
        }

        EventReportingThread eventReporter = new EventReportingThread();
        threads.add(eventReporter);
        eventReporter.start();

        processCollector = new ProcessCollectionThread();
        threads.add(processCollector);
        processCollector.start();

        CommandListenerThread commandListener = new CommandListenerThread(port);
        threads.add(commandListener);
        commandListener.start();

        Thread.sleep(15000);

        JsonNode pluginArray = config.get("plugins");
        if (pluginArray != null && pluginArray.isArray()) {
            for (int i = 0; i < pluginArray.size(); i++) {
                PluginThread pluginThread = new PluginThread(i, false);
                threads.add(pluginThread);
                pluginThread.start();
            }
        }

        JsonNode groupPluginArray = config.get("group-plugins");
        if (groupPluginArray != null && groupPluginArray.isArray()) {
            for (int i = 0; i < groupPluginArray.size(); i++) {
                GroupPluginThread groupPluginThread = new GroupPluginThread(i);
                threads.add(groupPluginThread);
                groupPluginThread.start();
            }
        }
    }

    // ── Event management ───────────────────────────────────────────────────────

    public static boolean fireEvent(String category, String subCategory, String itemName,
                                    String description, EventType status, ItemStyle style,
                                    Date currentRunTime) {
        if (style == ItemStyle.NoHealth) {
            Event evt = buildEvent(category, subCategory, itemName, description, status, style);
            EVENT_QUEUE.add(evt);
            return true;
        }

        String key = category + "|" + subCategory + "|" + itemName;
        ItemData itemData = getItemData(key);
        boolean overdue = checkOverdue(currentRunTime, itemData);

        if (itemData == null || status != itemData.lastStatus || overdue) {
            setItemData(key, new ItemData(status, currentRunTime));
            Event evt = buildEvent(category, subCategory, itemName, description, status, style);
            EVENT_QUEUE.add(evt);
            return true;
        }
        return false;
    }

    /**
     * Fires an event unconditionally, bypassing the status-change deduplication
     * check. Mirrors the behaviour of the original AbstractPlugin.sayStatusAlways.
     */
    static void fireEventAlways(String category, String subCategory, String itemName,
                                String description, EventType status, ItemStyle style) {
        Event evt = buildEvent(category, subCategory, itemName, description, status, style);
        EVENT_QUEUE.add(evt);
    }

    private static Event buildEvent(String category, String subCategory, String itemName,
                                    String description, EventType status, ItemStyle style) {
        Event evt = new Event();
        evt.Category    = category;
        evt.SubCategory = subCategory;
        evt.ItemName    = itemName;
        evt.StatusDescription = description;
        evt.Status      = status;
        evt.Style       = style;
        evt.Timestamp   = new Date();
        return evt;
    }

    private static void setItemData(String key, ItemData data) {
        synchronized (itemsLock) {
            items.put(key, data);
        }
    }

    private static ItemData getItemData(String key) {
        synchronized (itemsLock) {
            return items.get(key);
        }
    }

    private static boolean checkOverdue(Date currentRunTime, ItemData itemData) {
        if (itemData == null) return true;
        long millis = currentRunTime.getTime() - itemData.lastSent.getTime();
        long minutes = (millis / 1000) / 60;
        return minutes >= 10;
    }

    // ── Shutdown ───────────────────────────────────────────────────────────────

    public void stop() {
        if (monitorHubClient != null) {
            monitorHubClient.stop();
        }
        for (IThreadShutdown t : threads) {
            t.stop();
        }
    }

    // ── Config ─────────────────────────────────────────────────────────────────

    public static JsonNode readConfig() {
        synchronized (Monitor.class) {
            return MonitorProfile.getCurrent().getRootConfig();
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static String stackTrace(Exception e) {
        StringBuilder sb = new StringBuilder();
        for (StackTraceElement el : e.getStackTrace()) {
            sb.append("\tat ").append(el.toString()).append("\n");
        }
        return sb.toString();
    }

    // ── Inner types ────────────────────────────────────────────────────────────

    private static class ItemData {
        final EventType lastStatus;
        final Date lastSent;

        ItemData(EventType lastStatus, Date lastSent) {
            this.lastStatus = lastStatus;
            this.lastSent   = lastSent;
        }
    }
}
