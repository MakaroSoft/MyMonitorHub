using System;

namespace MyMonitorHub.Domain.Models
{
    public class ContactModel
    {
        public int ContactId { get; set; }
        public string Name { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string Phone3 { get; set; }
        public string Notes { get; set; }

        public int DeviceGroupId { get; set; }

        public int PhoneType1Id { get; set; }
        public int? PhoneType2Id { get; set; }
        public int? PhoneType3Id { get; set; }


        public string PhoneType1Name { get; set; }
        public string PhoneType2Name { get; set; }
        public string PhoneType3Name { get; set; }

    }
}