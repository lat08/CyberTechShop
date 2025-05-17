using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class Rank
    {
        public int RankId { get; set; }

        [Required]
        [StringLength(50)]
        public string RankName { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal MinTotalSpent { get; set; }

        [Range(0, 100)]
        public decimal? DiscountPercent { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int PriorityLevel { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        // Navigation properties
        public virtual ICollection<User> Users { get; set; }
    }
}