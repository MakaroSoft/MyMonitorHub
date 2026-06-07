namespace MyMonitorHub.Domain.Entity
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("Contact")]
    public partial class Contact
    {
        public int ContactId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Phone1 { get; set; } = string.Empty;

        public int PhoneType1Id { get; set; }

        [StringLength(20)]
        public string? Phone2 { get; set; }

        public int? PhoneType2Id { get; set; }

        [StringLength(20)]
        public string? Phone3 { get; set; }

        public int? PhoneType3Id { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        public int DeviceGroupId { get; set; }

        public virtual DeviceGroup DeviceGroup { get; set; } = null!;

        public virtual PhoneType PhoneType1 { get; set; } = null!;

        public virtual PhoneType? PhoneType2 { get; set; }

        public virtual PhoneType? PhoneType3 { get; set; }
    }
}
