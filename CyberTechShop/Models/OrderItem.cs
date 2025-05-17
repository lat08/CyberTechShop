using System;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class OrderItem
    {
        public int OrderItemID { get; set; }

        [Required]
        public int OrderID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public int? PromotionID { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Subtotal { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DiscountAmount { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal FinalSubtotal { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }
        public virtual Promotion Promotion { get; set; }
    }
}