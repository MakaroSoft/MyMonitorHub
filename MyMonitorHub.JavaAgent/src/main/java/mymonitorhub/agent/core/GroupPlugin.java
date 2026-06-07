package mymonitorhub.agent.core;

import com.fasterxml.jackson.databind.JsonNode;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
import java.util.List;

/**
 * A plugin that hosts multiple child plugins and runs them on their
 * individual hibernate schedules from a single thread.
 * Mirrors C# GroupPlugin.
 */
public class GroupPlugin extends AbstractPlugin {

    private static final Logger log = LoggerFactory.getLogger(GroupPlugin.class);
    private final List<AbstractPlugin> pluginList = new ArrayList<AbstractPlugin>();

    @Override
    public void initialize(JsonNode groupPluginElement, String threadTitle) {
        super.initialize(groupPluginElement, threadTitle);

        JsonNode pluginNodeList = groupPluginElement != null
                ? groupPluginElement.get("plugin") : null;
        if (pluginNodeList == null || !pluginNodeList.isArray()) return;

        for (int i = 0; i < pluginNodeList.size(); i++) {
            JsonNode pluginElement = pluginNodeList.get(i);
            String className = pluginElement.path("class-name").asText(null);
            if (className == null) continue;

            try {
                Class<?> clazz = Class.forName(className);
                Object instance = clazz.newInstance();
                if (!(instance instanceof AbstractPlugin)) {
                    throw new RuntimeException("class (" + className + ") must implement AbstractPlugin");
                }
                AbstractPlugin plugin = (AbstractPlugin) instance;
                plugin.initialize(pluginElement, getTitle());
                pluginList.add(plugin);
            } catch (Exception e) {
                log.error("Failed to load group plugin class {}: {}", className, e.getMessage());
            }
        }
    }

    public void displayPlugins() {
        for (AbstractPlugin plugin : pluginList) {
            log.info("    Plugin: {} - runs every {} seconds",
                    plugin.getTitle(), plugin.getHibernateSeconds());
        }
    }

    @Override
    protected void execute() {
        for (AbstractPlugin plugin : pluginList) {
            if (plugin.canRun()) {
                plugin.start();
            }
        }
    }
}
