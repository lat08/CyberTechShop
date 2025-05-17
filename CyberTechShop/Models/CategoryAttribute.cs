using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class CategoryAttribute
    {
        public int CategoryAttributeID { get; set; }

        [Required]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string AttributeName { get; set; }

        // Navigation property
        public virtual Category Category { get; set; }
    }
}