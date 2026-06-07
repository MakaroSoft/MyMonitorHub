using System.Collections.Generic;

namespace MyMonitorHub.Domain.Dto
{
    public class MonthlyReportModel
    {
        public List<MonthlyReportModelCompany> Companies;
        public int PageId;
        public string PageName;
    }

    public class MonthlyReportModelCompany
    {
        public int DeviceGroupId;
        public string Name;
        public bool IsGenerated;
        public bool IsEmailed;
        public string Path;
    }
}