using System;
using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class Item
    {
        public Item()
        {
            DiskUsages = new List<DiskUsage>();
            Errors = new List<Error>();
            UrlSpeeds = new List<UrlSpeed>();
            CpuUsages = new List<CpuUsage>();
        }

        public int ItemId { get; set; }
        public int CategoryId { get; set; }
        public string SubCategoryName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? EventId { get; set; }
        public int Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
        public int DeviceId { get; set; }
        public int? LastServiceRequestId { get; set; }
        public int AccountId { get; set; }
        public bool ConstantlyReportsInYN { get; set; }
        public virtual Category Category { get; set; } = null!;
        public virtual Device Device { get; set; } = null!;
        public virtual ServiceRequest? LastServiceRequest { get; set; }
        public virtual ICollection<DiskUsage> DiskUsages { get; set; }
        public virtual ICollection<Error> Errors { get; set; }
        public virtual ICollection<UrlSpeed> UrlSpeeds { get; set; }
        public virtual ICollection<CpuUsage> CpuUsages { get; set; }
    }
}
