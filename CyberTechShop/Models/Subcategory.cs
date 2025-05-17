using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class Subcategory
    {
        public int SubcategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required]
        public int CategoryID { get; set; }

        // Navigation properties
        public virtual Category Category { get; set; }
        public virtual ICollection<SubSubcategory> SubSubcategories { get; set; }
    }
}