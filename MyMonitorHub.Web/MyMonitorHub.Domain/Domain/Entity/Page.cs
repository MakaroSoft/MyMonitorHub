using System;
using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class Page
    {
        public Page()
        {
            this.DeviceGroups = new List<DeviceGroup>();
        }

        public int PageId { get; set; }
        public Nullable<int> ParentId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int ChildIndex { get; set; }
        public int AccountId { get; set; }
        public virtual ICollection<DeviceGroup> DeviceGroups { get; set; }
    }
}
