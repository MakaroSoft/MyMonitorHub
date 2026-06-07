using System.Runtime.Serialization;

namespace MyMonitorHub.Domain.BO.Transfer
{
    [DataContract]
    public class DeviceTransfer
    {
        [DataMember]
        public ColumnTransfer[] Columns;

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int DeviceId { get; set; }
    }
}
