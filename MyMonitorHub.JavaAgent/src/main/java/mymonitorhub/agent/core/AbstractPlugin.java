package mymonitorhub.agent.core;

import com.fasterxml.jackson.databind.JsonNode;
import mymonitorhub.agent.model.EventType;
import mymonitorhub.agent.model.ItemStyle;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Date;

/**
 * Base class for all monitoring plugins.
 * Mirrors C# AbstractPlugin.
 */
public abstract class AbstractPlugin {

    private static final Logger log = LoggerFactory.getLogger(AbstractPlugin.class);

    private int hibernateSeconds;
    private Date currentRunTime = new Date(0);
    private JsonNode pluginElement;
    private String title;

    public String getTitle() { return title; }

    public int getHibernateSeconds() { return hibernateSeconds; }

    public void setHibernateSeconds(int value) {
        hibernateSeconds = value;
        JsonNode hibernateNode = pluginElement != null ? pluginElement.get("hibernate-seconds") : null;
        if (hibernateNode != null && !hibernateNode.isNull()) {
            int fromConfig = hibernateNode.asInt(value);
            hibernateSeconds = Math.max(fromConfig, value);
        }
    }

    protected JsonNode getPluginElement() { return pluginElement; }

    public void initialize(JsonNode element, String threadTitle) {
        this.pluginElement = element;
        String pluginTitle = element != null ? element.path("title").asText(null) : null;
        if (pluginTitle != null) {
            this.title = pluginTitle.equals(threadTitle) ? pluginTitle : threadTitle + ">" + pluginTitle;
        } else {
            this.title = threadTitle;
        }
        setHibernateSeconds(120);
    }

    public boolean canRun() {
        if (currentRunTime.getTime() == 0) return true;
        long millis = new Date().getTime() - currentRunTime.getTime();
        long seconds = millis / 1000;
        return seconds >= hibernateSeconds;
    }

    public void start() {
        currentRunTime = new Date();
        log.debug("starting");
        execute();
    }

    protected abstract void execute();

    protected boolean sayStatus(String category, String subCategory, String name,
                                String description, EventType status) {
        return sayStatus(category, subCategory, name, description, status, ItemStyle.SendsHealth);
    }

    protected boolean sayStatus(String category, String subCategory, String name,
                                String description, EventType status, ItemStyle style) {
        return Monitor.fireEvent(category, subCategory, name, description, status, style, currentRunTime);
    }

    /**
     * Always fires the event unconditionally, bypassing the status-change
     * deduplication used by {@link #sayStatus}. Use when a state transition
     * must be reported regardless of the previous value (e.g. a counter reset).
     */
    protected final void sayStatusAlways(String category, String subCategory, String name,
                                         String description, EventType status) {
        sayStatusAlways(category, subCategory, name, description, status, ItemStyle.SendsHealth);
    }

    protected final void sayStatusAlways(String category, String subCategory, String name,
                                         String description, EventType status, ItemStyle style) {
        Monitor.fireEventAlways(category, subCategory, name, description, status, style);
    }
}
