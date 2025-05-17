using System;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class Payment
    {
        public int PaymentID { get; set; }

        [Required]
        public int OrderID { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        [RegularExpression("^(COD|VNPay|Momo)$")]
        public string PaymentMethod { get; set; }

        [Required]
        [StringLength(50)]
        [RegularExpression("^(Pending|Completed|Failed|Refunded)$")]
        public string PaymentStatus { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        // Navigation property
        public virtual Order Order { get; set; }
    }
}