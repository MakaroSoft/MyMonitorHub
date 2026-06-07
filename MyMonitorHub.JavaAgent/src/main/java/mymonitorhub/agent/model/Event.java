package mymonitorhub.agent.model;

import com.fasterxml.jackson.annotation.JsonFormat;

import java.util.Date;

/**
 * Mirrors MyMonitorHub.Common.WebApi.Event — must serialize identically for the server.
 */
public class Event {
    @JsonFormat(shape = JsonFormat.Shape.STRING, pattern = "yyyy-MM-dd'T'HH:mm:ss.SSSXXX")
    public Date Timestamp;
    public String Category;
    public String SubCategory;
    public String ItemName;
    public EventType Status;
    public ItemStyle Style;
    public String StatusDescription;

    public Event() {
    }
}
