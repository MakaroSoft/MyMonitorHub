using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class Category
    {
        public Category()
        {
            this.Items = new List<Item>();
        }

        public int CategoryId { get; set; }
        public int AccountId { get; set; }
        public string Description { get; set; } = string.Empty;
        public virtual ICollection<Item> Items { get; set; }
    }
}
