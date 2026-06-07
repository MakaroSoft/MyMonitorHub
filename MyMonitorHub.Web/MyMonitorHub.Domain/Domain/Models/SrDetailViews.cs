using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using MyMonitorHub.Domain.Util;
using Event = MyMonitorHub.Domain.Entity.Event;

namespace MyMonitorHub.Domain.Models
{
    public class SrDetailView
    {
        public List<SrDetailViewHistory> History;

        [DisplayName("Page")]
        public string PageName { get; set; }

        [DisplayName("Id")]
        public int ServiceRequestId { get; set; }

        public int DeviceId { get; set; }
        public ConnectionStatus? ConnectionStatus { get; set; }

        [DisplayName("Device")]
        public string DeviceDesc { get; set; }

        [DisplayName("Assigned To")]
        public string UserName { get; set; }

        [DisplayName("Notes")]
        public string Notes { get; set; }

        [DisplayName("Time")]
        public DateTime TimeStamp { get; set; }

        [DisplayName("Status")]
        public string Status { get; set; }

        [DisplayName("Device Group")]
        public string DeviceGroupName { get; set; }

        public int DeviceGroupId { get; set; }
        public string DeviceGroupNameLink { get; set; }
    } // class

    public class SrDetailViewHistory
    {
        public string Description { get; set; }
        public IGrouping<string, Event> Events { get; set; }
    } // class
} // namespace