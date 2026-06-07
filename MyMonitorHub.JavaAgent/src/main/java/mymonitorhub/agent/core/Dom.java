package mymonitorhub.agent.core;

import com.fasterxml.jackson.databind.JsonNode;

/**
 * JSON attribute helper — mirrors C# Dom.GetAttribute.
 */
public final class Dom {

    private Dom() {
    }

    public static String getAttribute(JsonNode node, String name) {
        return getAttribute(node, name, null);
    }

    public static String getAttribute(JsonNode node, String name, String defaultValue) {
        if (node == null || node.isMissingNode() || node.isNull()) {
            return defaultValue;
        }
        JsonNode child = node.get(name);
        if (child == null || child.isNull() || child.isMissingNode()) {
            return defaultValue;
        }
        String val = child.asText(defaultValue);
        return (val == null || val.isEmpty()) ? defaultValue : val;
    }
}
