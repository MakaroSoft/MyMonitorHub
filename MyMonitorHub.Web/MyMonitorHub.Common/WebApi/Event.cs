using System;

namespace MyMonitorHub.Common.WebApi
{

    /// <summary>
    /// Summary description for Event
    /// </summary>
    public class Event
    {
        public DateTime Timestamp;
        public string Category;
        public string SubCategory;
        public string ItemName; // must be unique within the categories
        public EventType Status;
        public ItemStyle Style;
        public string StatusDescription;
    }

    public enum Status
    {
        
    }
}