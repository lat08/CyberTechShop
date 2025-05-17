using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class ProductAttribute
    {
        public int AttributeID { get; set; }

        [Required]
        [StringLength(100)]
        public string AttributeName { get; set; }

        [Required]
        [StringLength(50)]
        public string AttributeType { get; set; }

        // Navigation properties
        public virtual ICollection<AttributeValue> AttributeValues { get; set; }
        public virtual ICollection<CategoryAttribute> CategoryAttributes { get; set; }
    }
}