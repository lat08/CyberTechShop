using System;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class UserAuthMethod
    {
        public int ID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        [RegularExpression("^(Password|Google|Facebook)$")]
        public string AuthType { get; set; }

        [Required]
        [StringLength(256)]
        public string AuthKey { get; set; }

        [StringLength(500)]
        public string? AuthSecret { get; set; }

        public string? AuthData { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? LastUsedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        // Navigation property
        public virtual User User { get; set; }
    }
}