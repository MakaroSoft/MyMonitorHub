package mymonitorhub.agent.plugins;

import mymonitorhub.agent.client.EventBroadcastClient;
import mymonitorhub.agent.core.AbstractPlugin;
import mymonitorhub.agent.core.Monitor;
import mymonitorhub.agent.model.EventType;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * Verifies the command listener thread is alive by sending a local MCM ping.
 * Mirrors C# CommandWatch.
 */
public class CommandWatch extends AbstractPlugin {

    private static final Logger log = LoggerFactory.getLogger(CommandWatch.class);

    private static final String CATEGORY     = "CMD PRC";
    private static final String SUB_CATEGORY = "Thread";
    private static final String NAME         = "Ping";

    @Override
    protected void execute() {
        EventBroadcastClient client = new EventBroadcastClient(Monitor.getPort());
        try {
            client.ping();
            log.debug("ok");
            sayStatus(CATEGORY, SUB_CATEGORY, NAME, "OK", EventType.Ok);
        } catch (Exception e) {
            handleFail(e);
        }
    }

    private void handleFail(Exception e) {
        Throwable cause = e.getCause() != null ? e.getCause() : e;
        String message = cause.getMessage();
        if (message == null || message.isEmpty()) {
            message = cause.toString();
        }
        sayStatus(CATEGORY, SUB_CATEGORY, NAME, message, EventType.Fail);
        log.error("CommandWatch execution failed: {}", message);
    }
}
