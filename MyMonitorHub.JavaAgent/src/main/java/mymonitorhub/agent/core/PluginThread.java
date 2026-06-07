package mymonitorhub.agent.core;

import com.fasterxml.jackson.databind.JsonNode;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Date;

/**
 * Runs a single plugin on its hibernate schedule.
 * Plugin classes are resolved via Class.forName() — the assembly config field
 * is ignored since all plugins ship inside the same fat JAR.
 * Mirrors C# PluginThread.
 */
public class PluginThread implements IThreadShutdown {

    private static final Logger log = LoggerFactory.getLogger(PluginThread.class);
    private static final int MINIMUM_SLEEP_MS = 30000;

    private volatile boolean stopped = true;
    private final Object padlock = new Object();
    private Thread thread;
    private Date lastRun = new Date(0);

    AbstractPlugin plugin;
    private String title;
    private final boolean multi;
    private final int index;

    public PluginThread(int index, boolean multi) {
        this.index = index;
        this.multi = multi;
        init();
    }

    private void init() {
        JsonNode config = Monitor.readConfig();
        JsonNode pluginArray = multi ? config.get("group-plugins") : config.get("plugins");
        if (pluginArray == null || !pluginArray.isArray() || index >= pluginArray.size()) {
            throw new RuntimeException("Plugin index " + index + " out of range");
        }
        JsonNode element = pluginArray.get(index);
        title = element.path("title").asText("plugin-" + index);
        String className = element.path("class-name").asText(null);
        if (className == null) {
            throw new RuntimeException("Plugin entry " + index + " has no class-name");
        }

        log.info("Loading plugin class: {}", className);
        try {
            Class<?> clazz = Class.forName(className);
            Object instance = clazz.newInstance();
            if (!(instance instanceof AbstractPlugin)) {
                throw new RuntimeException("class (" + className + ") must extend AbstractPlugin");
            }
            plugin = (AbstractPlugin) instance;
            plugin.initialize(element, title);
        } catch (ClassNotFoundException e) {
            throw new RuntimeException("Plugin class not found: " + className, e);
        } catch (InstantiationException | IllegalAccessException e) {
            throw new RuntimeException("Cannot instantiate plugin class: " + className, e);
        }
    }

    private int sleepTime() {
        long hibernateMs = plugin.getHibernateSeconds() * 1000L;
        long usedMs = new Date().getTime() - lastRun.getTime();
        long sleep = hibernateMs - usedMs;
        return (int) Math.max(sleep, MINIMUM_SLEEP_MS);
    }

    public void start() {
        thread = new Thread(new Runnable() {
            @Override
            public void run() {
                doRun();
            }
        });
        thread.setName("plugin-" + title);
        thread.setDaemon(true);
        thread.start();
    }

    @Override
    public void stop() {
        stopped = true;
        synchronized (padlock) {
            padlock.notifyAll();
        }
        if (thread != null) {
            try { thread.join(10000); } catch (InterruptedException ignored) {}
        }
    }

    private void doRun() {
        log.info("starting {} thread. Hibernation time = {} seconds.",
                title, plugin.getHibernateSeconds());
        if (multi) {
            ((GroupPlugin) plugin).displayPlugins();
        }
        stopped = false;

        while (!stopped) {
            lastRun = new Date();
            try {
                plugin.start();
            } catch (Exception e) {
                log.error("Plugin execution error: {}", e.getMessage());
            }

            int sleep = sleepTime();
            log.debug("going to sleep for {} seconds", sleep / 1000);
            try {
                synchronized (padlock) {
                    padlock.wait(sleep);
                }
            } catch (InterruptedException e) {
                stopped = true;
            }
        }
        log.info("{}: {} - has come to an end",
                multi ? "MultiPluginThread" : "PluginThread", title);
    }
}
