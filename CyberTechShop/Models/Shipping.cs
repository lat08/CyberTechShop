using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class Shipping
    {
        public int ShippingID { get; set; }

        [Required]
        public int OrderID { get; set; }

        [Required]
        [StringLength(255)]
        public string Address { get; set; }

        [Required]
        [StringLength(20)]
        public string Phone { get; set; }

        [Required]
        [StringLength(50)]
        [RegularExpression("^(Standard|Express)$")]
        public string ShippingMethod { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal ShippingCost { get; set; }

        [Required]
        [StringLength(50)]
        [RegularExpression("^(Pending|Shipped|InTransit|Delivered)$")]
        public string Status { get; set; }

        // Navigation property
        public virtual Order Order { get; set; }
    }
}