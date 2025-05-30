using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CyberTech.Models
{
    public class Voucher
    {
        [Key]
        public int VoucherID { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }

        public string Description { get; set; }

        [Required]
        [StringLength(10)]
        public string DiscountType { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than 0")]
        public decimal DiscountValue { get; set; }

        public int? QuantityAvailable { get; set; }

        [Required]
        public DateTime ValidFrom { get; set; }

        [Required]
        public DateTime ValidTo { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        [StringLength(10)]
        public string AppliesTo { get; set; } = "Order";

        public virtual ICollection<VoucherProducts> VoucherProducts { get; set; }
    }
}