using CyberTech.Models;

namespace CyberTech.ViewModels
{
    public class CartViewModel
    {
        public Cart Cart { get; set; }
        public List<CartItem> CartItems { get; set; }
        public List<UserAddress> UserAddresses { get; set; }
        public Voucher AppliedVoucher { get; set; }
    }
}