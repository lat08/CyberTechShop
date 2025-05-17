using System;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class UserAddress
    {
        public int AddressID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(255)]
        public string AddressLine { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? District { get; set; }

        [StringLength(100)]
        public string? Ward { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        public bool IsPrimary { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation property
        public virtual User User { get; set; }
    }
}