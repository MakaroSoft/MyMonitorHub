using System;
using System.Collections.Generic;
using MyMonitorHub.Domain.Util;

namespace MyMonitorHub.Domain.Models
{
    public class DeviceLayoutModel
    {
        public string Description;
        public int DeviceId;
        public string WebUrl;
        public string WebPingUrl;
        public string Notes;
        public string DeviceGroupNotes;
        public ConnectionStatus? ConnectionStatus;
        public List<DeviceLayoutModelItemGroup> ItemGroups = new List<DeviceLayoutModelItemGroup>();
        public int? DeviceGroupId { get; set; }
        public string DeviceGroupName { get; set; }
        public string CleanDescription { get; set; }

        public List<KeyValuePair<int,string>> OtherDevices = new List<KeyValuePair<int, string>>();

        public string DeviceName;
        public string GroupDescriptionHtml;
    }

    public class DeviceLayoutModelItemGroup
    {
        public string Description;
        public IEnumerable<DeviceLayoutModelItem> Items;
    } // class

    public class DeviceLayoutModelItem
    {
        public int ItemId;
        public bool ConstantlyReportsIn;
        public string Description;
        public int Status;
        public string StatusDescription;
        public DateTime Timestamp;

        public string Age
        {
            get
            {
                var timespan = DateTime.Now - Timestamp;
                var result = "";
                var days = timespan.Days;
                if (days > 0) result = result + days + " days, ";
                var hours = timespan.Hours;
                if (hours > 0) result = result + hours + " hours, ";
                var minutes = timespan.Minutes;
                result = result + minutes + " minutes";
                return result;
            }
        }

        public string BroadcastImage
        {
            get
            {
                if (ConstantlyReportsIn)
                {
                    return "<span class='ms-icon16 ms-icon16-style-sends-health'></span>";
                }
                return "";
            }
        }

        public string LateImage
        {
            get
            {
                var timespan = DateTime.Now - Timestamp;
                if (ConstantlyReportsIn && timespan.TotalMinutes > 15)
                {
                    return "<span class='ms-icon16 ms-icon16-really-late'></span>";
                }
                return "";
            }
        }
    }
} // namespace