package mymonitorhub.agent.client;

import mymonitorhub.agent.core.McmProtocol;

import java.net.InetAddress;
import java.net.Socket;
import java.nio.charset.Charset;

/**
 * Local client that sends MCM commands to the agent's loopback command port.
 * Used by CommandWatch to verify the command listener is alive.
 * Mirrors C# EventBroadcastClient.
 */
public class EventBroadcastClient {

    private final int port;

    public EventBroadcastClient(int port) {
        this.port = port;
    }

    /**
     * Sends a ping command. Throws if the command listener does not respond.
     */
    public void ping() throws Exception {
        try (Socket socket = new Socket(InetAddress.getLoopbackAddress(), port)) {
            socket.setSoTimeout(10000);
            McmProtocol protocol = new McmProtocol(socket);
            protocol.write("Command: ping\r\n", null);
            protocol.read();
            String status = protocol.getHeaderParam("Status: ");
            if (!"OK".equalsIgnoreCase(status != null ? status.trim() : "")) {
                throw new RuntimeException("Ping returned non-OK status: " + status);
            }
        }
    }

    /**
     * Fires a monitoring event into the agent queue via the local command port.
     */
    public void fireEvent(String group, String category, String name,
                          String description, String status, String style) throws Exception {
        try (Socket socket = new Socket(InetAddress.getLoopbackAddress(), port)) {
            socket.setSoTimeout(10000);
            McmProtocol protocol = new McmProtocol(socket);
            String params = "Command: fireEvent\r\n"
                    + "Group: "       + group       + "\r\n"
                    + "Category: "    + category    + "\r\n"
                    + "Name: "        + name        + "\r\n"
                    + "Description: " + description + "\r\n"
                    + "Status: "      + status      + "\r\n"
                    + "Style: "       + style       + "\r\n";
            protocol.write(params, null);
            protocol.read();
            String responseStatus = protocol.getHeaderParam("Status: ");
            if (!"OK".equalsIgnoreCase(responseStatus != null ? responseStatus.trim() : "")) {
                throw new RuntimeException("fireEvent returned non-OK status: " + responseStatus);
            }
        }
    }
}
