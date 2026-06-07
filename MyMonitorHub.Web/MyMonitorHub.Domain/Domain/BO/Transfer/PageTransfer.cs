using System.Runtime.Serialization;

namespace MyMonitorHub.Domain.BO.Transfer
{
    [DataContract]
    public class PageTransfer
    {
        [DataMember]
        public ColumnHeaderTransfer[] ColumnHeaders;
        [DataMember]
        public DeviceGroupTransfer[] DeviceGroups;
    }
}
