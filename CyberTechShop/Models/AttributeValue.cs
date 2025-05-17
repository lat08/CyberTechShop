using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class AttributeValue
    {
        public int ValueID { get; set; }

        [Required]
        public int AttributeID { get; set; }

        [Required]
        [StringLength(255)]
        public string ValueName { get; set; }

        // Navigation properties
        public virtual Attribute Attribute { get; set; }
        public virtual ICollection<ProductAttributeValue> ProductAttributeValues { get; set; }
    }
}