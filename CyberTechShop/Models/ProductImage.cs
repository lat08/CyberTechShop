using System;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class ProductImage
    {
        public int ImageID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        [StringLength(255)]
        public string ImageURL { get; set; }

        [Required]
        public bool IsPrimary { get; set; }

        [Required]
        public int DisplayOrder { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation property
        public virtual Product Product { get; set; }
    }
}