package mymonitorhub.agent.core;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.Socket;

/**
 * Listens for inbound MCM connections on the loopback interface.
 * Each accepted connection is handed off to a CommandProcessingThread.
 * Mirrors C# CommandListenerThread.
 */
class CommandListenerThread implements IThreadShutdown {

    private static final Logger log = LoggerFactory.getLogger(CommandListenerThread.class);

    private final int port;
    private volatile boolean stopped = true;
    private ServerSocket serverSocket;
    private Thread thread;

    CommandListenerThread(int port) {
        this.port = port;
    }

    void start() {
        thread = new Thread(new Runnable() {
            @Override
            public void run() {
                doRun();
            }
        });
        thread.setName("cmd-listener");
        thread.setDaemon(true);
        thread.start();
    }

    @Override
    public void stop() {
        stopped = true;
        try {
            if (serverSocket != null && !serverSocket.isClosed()) {
                serverSocket.close();
            }
        } catch (Exception ignored) {}
        if (thread != null) {
            try { thread.join(10000); } catch (InterruptedException ignored) {}
        }
    }

    private void doRun() {
        stopped = false;
        int errorCount = 0;

        while (!stopped) {
            log.info("Command listener Started on Port: {}", port);
            try {
                // Bind to loopback only — same security rationale as C# version.
                serverSocket = new ServerSocket(port, 100, InetAddress.getLoopbackAddress());
                errorCount = 0;

                while (!stopped) {
                    Socket client = serverSocket.accept();
                    CommandProcessingThread handler = new CommandProcessingThread(client);
                    handler.start();
                }
            } catch (Exception e) {
                if (!stopped) {
                    errorCount++;
                    log.error("Command listener error: {}", e.getMessage());
                    if (errorCount >= 2) {
                        stopped = true;
                        break;
                    }
                    try { Thread.sleep(10000); } catch (InterruptedException ignored) {}
                }
            } finally {
                if (serverSocket != null && !serverSocket.isClosed()) {
                    try { serverSocket.close(); } catch (Exception ignored) {}
                }
            }
        }
        log.info("CommandListenerThread has come to an end");
    }
}
