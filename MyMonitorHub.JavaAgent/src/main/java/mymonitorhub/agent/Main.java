package mymonitorhub.agent;

import mymonitorhub.agent.core.Monitor;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * Console entry point for the MyMonitorHub Java Agent.
 *
 * Run:
 *   java -jar mymonitorhub-java-agent-1.0.0.jar
 *
 * Config is read from appsettings.json in the current working directory,
 * or from the directory pointed to by the MYMONITORHUB_AGENT_CONFIG_DIR
 * environment variable.
 *
 * Mirrors C# AgentWorker / AgentServiceManager startup sequence.
 */
public class Main {

    private static final Logger log = LoggerFactory.getLogger(Main.class);

    public static void main(String[] args) {
        log.info("MyMonitorHub Java Agent starting...");

        final Monitor monitor = new Monitor();

        // Graceful shutdown hook — mirrors C# AgentWorker.StopAsync
        Runtime.getRuntime().addShutdownHook(new Thread(new Runnable() {
            @Override
            public void run() {
                log.info("Shutdown signal received — stopping agent...");
                monitor.stop();
                log.info("Agent stopped.");
            }
        }));

        // Run on the current (main) thread — blocks until the process is killed
        // or Runtime.halt() is called by EventReportingThread.
        monitor.run();
    }
}
