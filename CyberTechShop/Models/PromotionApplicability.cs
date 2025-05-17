using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class PromotionApplicability
    {
        public int PromotionApplicabilityID { get; set; }

        [Required]
        public int PromotionID { get; set; }

        public int? ProductID { get; set; }

        public int? CategoryID { get; set; }

        // Navigation properties
        public virtual Promotion Promotion { get; set; }
        public virtual Product Product { get; set; }
        public virtual Category Category { get; set; }
    }
}