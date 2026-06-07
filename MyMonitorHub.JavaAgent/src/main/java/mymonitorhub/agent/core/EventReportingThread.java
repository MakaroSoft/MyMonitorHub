package mymonitorhub.agent.core;

import mymonitorhub.agent.common.MonitorProfile;
import mymonitorhub.agent.common.WebApiCall;
import mymonitorhub.agent.model.Event;
import mymonitorhub.agent.model.EventPacket;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.Date;
import java.util.List;

/**
 * Wakes every 60 seconds, drains the event queue, and POSTs to the hub.
 * If events fail to send for longer than reconnect-attempt-minutes the
 * process is killed so the OS can restart it.
 * Mirrors C# EventReportingThread.
 */
class EventReportingThread implements IThreadShutdown {

    private static final Logger log = LoggerFactory.getLogger(EventReportingThread.class);

    private volatile boolean stopped = true;
    private final Object padlock = new Object();
    private Date lastSuccess;
    private Thread thread;

    void start() {
        thread = new Thread(new Runnable() {
            @Override
            public void run() {
                doRun();
            }
        });
        thread.setName("event-reporter");
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
        lastSuccess = new Date();
        log.info("Start of Event reporting thread");
        stopped = false;

        while (!stopped) {
            try {
                synchronized (padlock) {
                    padlock.wait(60000);
                }
                if (stopped) break;
            } catch (InterruptedException e) {
                stopped = true;
                break;
            }

            Date start = new Date();
            List<Event> eventList = Monitor.getEventList();

            if (!eventList.isEmpty()) {
                EventPacket packet = new EventPacket(eventList.toArray(new Event[0]));
                try {
                    MonitorProfile profile = MonitorProfile.getCurrent();
                    WebApiCall service = new WebApiCall(profile.getBaseAddress(), MonitorProfile.getTokenManager());
                    service.send(profile.getAccountId(), profile.getDeviceId(), packet);
                    lastSuccess = new Date();
                } catch (Exception e) {
                    log.error("Choked on sending the event: {}", e.getMessage());
                    if (e.getCause() != null) {
                        log.error("Inner Exception: {}", e.getCause().getMessage());
                    }
                    displayDuration(start, true);

                    int reconnectMinutes = MonitorProfile.getCurrent().getReconnectAttemptMinutes();
                    if (reconnectMinutes > 0) {
                        long diffMs = new Date().getTime() - lastSuccess.getTime();
                        long diffMinutes = diffMs / 60000;
                        if (diffMinutes >= reconnectMinutes) {
                            log.error("*** Messages are not getting out! Trying to restart the process!");
                            try { Thread.sleep(2000); } catch (InterruptedException ignored) {}
                            synchronized (Monitor.FILE_WRITE_LOCK) {
                                Runtime.getRuntime().halt(1);
                            }
                        }
                    }
                }
            }
        }
        log.info("Event reporting thread has come to an end");
    }

    private void displayDuration(Date start, boolean force) {
        double seconds = (new Date().getTime() - start.getTime()) / 1000.0;
        if (seconds > 30 || force) {
            log.info("time in seconds to execute the request: {}", seconds);
        }
    }
}
