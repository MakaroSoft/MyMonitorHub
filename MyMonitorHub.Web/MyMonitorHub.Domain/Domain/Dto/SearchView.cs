using System;
using System.Collections.Generic;
using MyMonitorHub.Domain.Util;

namespace MyMonitorHub.Domain.Dto
{
    public class PageDefaults
    {
        public SearchCriteria Criteria;
        public int Page;
        public int Rows;
        public string Sidx;
        public string Sord;
    }

    public class SearchView
    {
        public List<TextValuePair> Accounts;
        public List<TextValuePair> AssignedTos;
        public SearchCriteria Criteria;
        public List<TextValuePair> DeviceGroups;
        public int Page;
        public int Rows;

        public string Sidx;
        public string Sord;
    }

    public class SearchCriteria
    {
        public bool Search { get; set; }
        public int? SrFrom { get; set; }
        public int? SrTo { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public int SelectedAccount { get; set; }

        public int SelectedAssignedTo { get; set; }
        public int SelectedDeviceGroup { get; set; }
    }
}