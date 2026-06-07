using System.Runtime.Serialization;

namespace MyMonitorHub.Domain.BO.Transfer
{
    [DataContract]
    public class ColumnTransfer
    {
        [DataMember]
        public string Code { get; set; }
    }
}
