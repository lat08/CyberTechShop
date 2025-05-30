using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTech.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        // Navigation property
        public virtual ICollection<Subcategory> Subcategories { get; set; }
    }
}