package mymonitorhub.agent.core;

import mymonitorhub.agent.model.Event;

import java.util.ArrayList;
import java.util.List;

/**
 * Thread-safe event queue. Draining swaps out the list atomically.
 * Mirrors C# EventQueue.
 */
class EventQueue {

    private List<Event> eventList = new ArrayList<Event>();

    /** Atomically drains and returns all queued events, leaving an empty queue. */
    synchronized List<Event> drain() {
        List<Event> result = eventList;
        eventList = new ArrayList<Event>();
        return result;
    }

    synchronized void add(Event event) {
        eventList.add(event);
    }
}
