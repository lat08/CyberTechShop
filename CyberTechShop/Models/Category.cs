using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class Category
    {
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<Subcategory> Subcategories { get; set; }
        public virtual ICollection<CategoryAttribute> CategoryAttributes { get; set; }
        public virtual ICollection<PromotionApplicability> PromotionApplicabilities { get; set; }
    }
}