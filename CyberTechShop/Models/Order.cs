using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class Order
    {
        public int OrderID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DiscountAmount { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal FinalPrice { get; set; }

        [Required]
        [StringLength(50)]
        [RegularExpression("^(Pending|Processing|Shipped|Delivered|Cancelled)$")]
        public string Status { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual Payment Payment { get; set; }
        public virtual Shipping Shipping { get; set; }
    }
}