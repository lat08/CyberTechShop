using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class Wishlist
    {
        public int WishlistID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual ICollection<WishlistItem> WishlistItems { get; set; }
    }
}