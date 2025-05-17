using System;
using System.ComponentModel.DataAnnotations;

namespace CyberTechShop.Models
{
    public class WishlistItem
    {
        public int WishlistItemID { get; set; }

        [Required]
        public int WishlistID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        public DateTime AddedAt { get; set; }

        // Navigation properties
        public virtual Wishlist Wishlist { get; set; }
        public virtual Product Product { get; set; }
    }
}