using System.Runtime.Serialization;

namespace MyMonitorHub.Domain.BO.Transfer
{
    [DataContract]
    public class DeviceGroupTransfer
    {
        [DataMember]
        public int Id { get; set; }


        [DataMember]
        public DeviceTransfer[] Devices;

        [DataMember]
        public string Description { get; set; }
    }
}
