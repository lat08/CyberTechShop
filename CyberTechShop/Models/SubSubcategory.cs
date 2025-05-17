using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class SubSubcategory
    {
        public int SubSubcategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required]
        public int SubcategoryID { get; set; }

        // Navigation properties
        public virtual Subcategory Subcategory { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}