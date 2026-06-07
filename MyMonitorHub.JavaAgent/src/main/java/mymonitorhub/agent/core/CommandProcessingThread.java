package mymonitorhub.agent.core;

import mymonitorhub.agent.model.EventType;
import mymonitorhub.agent.model.ItemStyle;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.IOException;
import java.net.Socket;
import java.nio.charset.Charset;
import java.util.Date;

/**
 * Handles a single inbound MCM connection.
 * Supports commands: fireEvent, ping.
 * Mirrors C# CommandProcessingThread.
 */
class CommandProcessingThread {

    private static final Logger log = LoggerFactory.getLogger(CommandProcessingThread.class);

    private final Socket socket;
    private McmProtocol mcmProtocol;

    CommandProcessingThread(Socket socket) {
        this.socket = socket;
    }

    void start() {
        Thread t = new Thread(new Runnable() {
            @Override
            public void run() {
                doRun();
            }
        });
        t.setName("cmd-processor");
        t.setDaemon(true);
        t.start();
    }

    private void doRun() {
        try {
            socket.setSoTimeout(30000);
            mcmProtocol = new McmProtocol(socket);
            mcmProtocol.read();

            String data = mcmProtocol.getHeader();
            String command = getData(data, "Command: ");
            if (command == null) {
                sayFailed("Missing 'Command' parameter");
                return;
            }

            switch (command) {
                case "fireEvent":
                    fireEventCommand(data);
                    break;
                case "ping":
                    sayPassed();
                    break;
                default:
                    sayFailed("Unrecognized command: " + command);
            }
        } catch (Exception e) {
            log.error("run> {}\n{}", e.getMessage(), e.getStackTrace());
        } finally {
            try { socket.close(); } catch (IOException ignored) {}
        }
    }

    private void fireEventCommand(String data) throws IOException {
        String group    = getData(data, "Group: ");
        String category = getData(data, "Category: ");
        String name     = getData(data, "Name: ");
        String desc     = getData(data, "Description: ");
        String status   = getData(data, "Status: ");
        String style    = getData(data, "Style: ");

        if (group == null || category == null || name == null
                || desc == null || status == null || style == null) {
            sayFailed("Missing a parameter");
            return;
        }

        EventType stat;
        switch (status.toUpperCase()) {
            case "OK":   stat = EventType.Ok;   break;
            case "FAIL": stat = EventType.Fail; break;
            default:
                sayFailed("Invalid status code.");
                return;
        }

        ItemStyle styleEnum;
        switch (style.toUpperCase()) {
            case "NO_HEALTH":    styleEnum = ItemStyle.NoHealth;    break;
            case "SENDS_HEALTH": styleEnum = ItemStyle.SendsHealth; break;
            default:
                sayFailed("Invalid style code.");
                return;
        }

        Monitor.fireEvent(group, category, name, desc, stat, styleEnum, new Date());
        sayPassed();
    }

    private static String getData(String data, String searchFor) {
        String marker = "\r\n" + searchFor;
        int start = data.indexOf(marker);
        if (start == -1) return null;
        int valueStart = start + marker.length();
        int end = data.indexOf("\r\n", valueStart);
        if (end == -1) return null;
        return data.substring(valueStart, end);
    }

    private void sayPassed() throws IOException {
        mcmProtocol.write("Status: OK\r\n", null);
    }

    private void sayFailed(String reason) throws IOException {
        mcmProtocol.write("Status: Fail\r\n", reason.getBytes(Charset.forName("UTF-8")));
    }
}
