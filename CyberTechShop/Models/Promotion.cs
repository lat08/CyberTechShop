using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class Promotion
    {
        public int PromotionID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        [RegularExpression("^(Percentage|FixedAmount)$")]
        public string DiscountType { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal DiscountValue { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public bool IsActive { get; set; }

        // Navigation properties
        public virtual ICollection<PromotionApplicability> PromotionApplicabilities { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}