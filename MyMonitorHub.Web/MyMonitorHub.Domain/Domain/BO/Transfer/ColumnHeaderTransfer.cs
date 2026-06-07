using System.Runtime.Serialization;

namespace MyMonitorHub.Domain.BO.Transfer
{
    [DataContract]
    public class ColumnHeaderTransfer
    {
        [DataMember]
        public string Description { get; set; }
    }
}
