package mymonitorhub.agent.model;

/**
 * Mirrors MyMonitorHub.Common.WebApi.EventPacket — the payload POSTed to the server.
 */
public class EventPacket {
    public Event[] Events;

    public EventPacket() {
    }

    public EventPacket(Event[] events) {
        this.Events = events;
    }
}
