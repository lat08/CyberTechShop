using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberTech.Models
{
    public class Product
    {
        public Product()
        {
            ProductImages = new List<ProductImage>();
            ProductAttributeValues = new List<ProductAttributeValue>();
            Wishlists = new List<Wishlist>();
            CartItems = new List<CartItem>();
            OrderItems = new List<OrderItem>();
            Reviews = new List<Review>();
            VoucherProducts = new List<VoucherProducts>();
        }

        [Key]
        public int ProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? SalePercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalePrice { get; set; }

        [Required]
        public int Stock { get; set; }

        [Required]
        public int SubSubcategoryID { get; set; }

        [ForeignKey("SubSubcategoryID")]
        public virtual SubSubcategory SubSubcategory { get; set; }

        [StringLength(100)]
        public string Brand { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<ProductImage> ProductImages { get; set; }
        public virtual ICollection<ProductAttributeValue> ProductAttributeValues { get; set; }
        public virtual ICollection<Wishlist> Wishlists { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<VoucherProducts> VoucherProducts { get; set; }
    }
}