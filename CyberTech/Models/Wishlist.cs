using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CyberTech.Models
{
    public class Wishlist
    {
        [Key]
        public int WishlistID { get; set; }

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        public virtual ICollection<WishlistItem> WishlistItems { get; set; }
    }

    public class WishlistItem
    {
        [Key]
        public int WishlistItemID { get; set; }

        [Required]
        public int WishlistID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        public DateTime AddedDate { get; set; } = DateTime.Now;

        [Required]
        public int Quantity { get; set; } = 1;

        [ForeignKey("WishlistID")]
        public virtual Wishlist Wishlist { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}