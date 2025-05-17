using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class ProductAttributeValue
    {
        [Required]
        public int ProductID { get; set; }

        [Required]
        public int ValueID { get; set; }

        // Navigation properties
        public virtual Product Product { get; set; }
        public virtual AttributeValue AttributeValue { get; set; }
    }
}