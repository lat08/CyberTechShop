using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class User
    {
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(500)]
        public string? ProfileImageURL { get; set; }

        [Required]
        [StringLength(20)]
        [RegularExpression("^(Customer|Support|Manager|SuperAdmin)$")]
        public string Role { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Salary { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalSpent { get; set; }

        [Range(0, int.MaxValue)]
        public int OrderCount { get; set; }

        public int? RankId { get; set; }

        public bool EmailVerified { get; set; }

        [Required]
        [StringLength(20)]
        [RegularExpression("^(Active|Inactive|Suspended)$")]
        public string UserStatus { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public virtual Rank Rank { get; set; }
        public virtual ICollection<UserAuthMethod> UserAuthMethods { get; set; }
        public virtual ICollection<UserAddress> UserAddresses { get; set; }
        public virtual ICollection<Cart> Carts { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<Wishlist> Wishlists { get; set; }
    }
}