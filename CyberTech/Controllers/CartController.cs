using CyberTech.Data;
using CyberTech.Models;
using CyberTech.Services;
using CyberTech.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CyberTech.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, IUserService userService, ILogger<CartController> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        private decimal CalculateCartTotals(Cart cart, Voucher voucher, out decimal discountAmount)
        {
            decimal subtotal = cart.CartItems.Sum(ci => ci.Subtotal);
            discountAmount = 0;

            if (voucher != null && IsVoucherValid(voucher, cart))
            {
                discountAmount = CalculateVoucherDiscount(voucher, cart);
            }

            return subtotal - discountAmount;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Unauthorized("Invalid user identifier.");
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null) return NotFound();

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.ProductImages)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null)
                {
                    cart = new Cart { UserID = user.UserID, TotalPrice = 0 };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var userAddresses = await _userService.GetUserAddressesAsync(user.UserID);

                var appliedVoucherId = HttpContext.Session.GetInt32("AppliedVoucherId");
                Voucher appliedVoucher = null;
                if (appliedVoucherId.HasValue)
                {
                    appliedVoucher = await _context.Vouchers
                        .Include(v => v.VoucherProducts)
                        .FirstOrDefaultAsync(v => v.VoucherID == appliedVoucherId.Value);
                }

                decimal discountAmount;
                cart.TotalPrice = CalculateCartTotals(cart, appliedVoucher, out discountAmount);

                var model = new CartViewModel
                {
                    Cart = cart,
                    CartItems = cart.CartItems.ToList(),
                    UserAddresses = userAddresses,
                    AppliedVoucher = appliedVoucher
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart for user {Email}", User.Identity.Name);
                return View(new CartViewModel());
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null || product.Stock < quantity)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc không đủ hàng" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null)
                {
                    cart = new Cart { UserID = user.UserID, TotalPrice = 0 };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductID == productId);
                if (cartItem != null)
                {
                    cartItem.Quantity += quantity;
                    if (cartItem.Quantity > product.Stock)
                    {
                        cartItem.Quantity = product.Stock;
                    }
                    cartItem.Subtotal = cartItem.Quantity * product.GetEffectivePrice();
                }
                else
                {
                    cartItem = new CartItem
                    {
                        CartID = cart.CartID,
                        ProductID = productId,
                        Quantity = quantity,
                        Subtotal = quantity * product.GetEffectivePrice()
                    };
                    cart.CartItems.Add(cartItem);
                }

                cart.TotalPrice = cart.CartItems.Sum(ci => ci.Subtotal);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã thêm vào giỏ hàng", cart = new { totalPrice = cart.TotalPrice, cartItemsCount = cart.CartItems.Count } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart for product {ProductId}", productId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi thêm vào giỏ hàng" });
            }
        }

        // Helper method to calculate the effective price based on sale options
        private decimal CalculateEffectivePrice(Product product)
        {
            return product.GetEffectivePrice();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy giỏ hàng" });
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);
                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                cart.CartItems.Remove(cartItem);
                cart.TotalPrice = cart.CartItems.Sum(ci => ci.Subtotal);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng", cart = new { totalPrice = cart.TotalPrice, cartItemsCount = cart.CartItems.Count } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item {CartItemId}", cartItemId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sản phẩm" });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null || !cart.CartItems.Any())
                {
                    return Json(new { success = true, message = "Giỏ hàng đã trống" });
                }

                _context.CartItems.RemoveRange(cart.CartItems);
                cart.TotalPrice = 0;
                HttpContext.Session.Remove("AppliedVoucherId");
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã xóa toàn bộ giỏ hàng" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {Email}", User.Identity.Name);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa giỏ hàng" });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                Console.WriteLine($"\n=== DEBUG: Updating quantity for cart item {cartItemId} to {quantity} ===");

                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy giỏ hàng" });
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.CartItemID == cartItemId);
                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                if (quantity <= 0)
                {
                    return Json(new { success = false, message = "Số lượng phải lớn hơn 0" });
                }

                if (quantity > cartItem.Product.Stock)
                {
                    return Json(new { success = false, message = "Số lượng vượt quá tồn kho" });
                }

                cartItem.Quantity = quantity;
                cartItem.Subtotal = quantity * cartItem.Product.GetEffectivePrice();

                var appliedVoucherId = HttpContext.Session.GetInt32("AppliedVoucherId");
                Voucher appliedVoucher = null;
                if (appliedVoucherId.HasValue)
                {
                    appliedVoucher = await _context.Vouchers
                        .Include(v => v.VoucherProducts)
                        .FirstOrDefaultAsync(v => v.VoucherID == appliedVoucherId.Value);
                }

                decimal discountAmount;
                cart.TotalPrice = CalculateCartTotals(cart, appliedVoucher, out discountAmount);

                await _context.SaveChangesAsync();

                Console.WriteLine($"Updated cart item subtotal: {cartItem.Subtotal}");
                Console.WriteLine($"Cart total after update: {cart.TotalPrice}");
                Console.WriteLine($"Discount amount: {discountAmount}");

                return Json(new
                {
                    success = true,
                    message = "Đã cập nhật số lượng",
                    subtotal = cartItem.Subtotal,
                    cart = new
                    {
                        subtotal = cart.CartItems.Sum(ci => ci.Subtotal),
                        discountAmount = discountAmount,
                        totalPrice = cart.TotalPrice,
                        cartItemsCount = cart.CartItems.Count
                    },
                    discountType = appliedVoucher?.DiscountType ?? "FIXED",
                    discountPercent = appliedVoucher?.DiscountType == "PERCENT" ? appliedVoucher.DiscountValue : 0
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateQuantity: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật số lượng" });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher(string voucherCode)
        {
            try
            {
                Console.WriteLine($"\n=== DEBUG: Applying Voucher {voucherCode} ===");
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    Console.WriteLine("Error: Email claim not found");
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    Console.WriteLine("Error: User not found");
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }
                Console.WriteLine($"User found: {user.UserID}");

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null || !cart.CartItems.Any())
                {
                    Console.WriteLine("Error: Cart is empty");
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }
                Console.WriteLine($"Cart found with {cart.CartItems.Count} items");

                var voucher = await _context.Vouchers
                    .Include(v => v.VoucherProducts)
                    .FirstOrDefaultAsync(v => v.Code == voucherCode && v.IsActive && v.ValidFrom <= DateTime.Now && v.ValidTo >= DateTime.Now);

                if (voucher == null)
                {
                    Console.WriteLine("Error: Invalid or expired voucher");
                    return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn" });
                }
                Console.WriteLine($"Voucher found: {voucher.Code}, Type: {voucher.DiscountType}, Value: {voucher.DiscountValue}");

                if (voucher.QuantityAvailable.HasValue && voucher.QuantityAvailable <= 0)
                {
                    Console.WriteLine("Error: Voucher quantity exhausted");
                    return Json(new { success = false, message = "Mã giảm giá đã hết số lượng sử dụng" });
                }

                if (!IsVoucherValid(voucher, cart))
                {
                    Console.WriteLine("Error: Voucher not applicable to cart items");
                    return Json(new { success = false, message = "Mã giảm giá không áp dụng cho sản phẩm trong giỏ hàng" });
                }

                decimal subtotal = cart.CartItems.Sum(ci => ci.Subtotal);
                Console.WriteLine($"Cart subtotal before discount: {subtotal:C}");

                // Calculate discount amount based on discount type
                decimal discountAmount = CalculateVoucherDiscount(voucher, cart);
                decimal finalTotal = subtotal - discountAmount;

                Console.WriteLine($"Calculated discount amount: {discountAmount:C}");
                Console.WriteLine($"Final total after discount: {finalTotal:C}");

                HttpContext.Session.SetInt32("AppliedVoucherId", voucher.VoucherID);
                cart.TotalPrice = finalTotal;
                await _context.SaveChangesAsync();

                Console.WriteLine("=== Voucher Application Complete ===\n");

                return Json(new
                {
                    success = true,
                    message = "Đã áp dụng mã giảm giá",
                    cart = new
                    {
                        totalPrice = finalTotal,
                        subtotal = subtotal,
                        discountAmount = discountAmount,
                        cartItemsCount = cart.CartItems.Count
                    },
                    discountType = voucher.DiscountType,
                    discountPercent = voucher.DiscountType == "PERCENT" ? voucher.DiscountValue : 0,
                    voucherCode = voucher.Code
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ApplyVoucher: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi áp dụng mã giảm giá" });
            }
        }

        private decimal CalculateVoucherDiscount(Voucher voucher, Cart cart)
        {
            decimal cartTotal = cart.CartItems.Sum(ci => ci.Subtotal);
            decimal discountAmount = 0;
            Console.WriteLine($"\n=== DEBUG: Calculating Voucher Discount ===");
            Console.WriteLine($"Cart Total: {cartTotal:C}");
            Console.WriteLine($"Voucher Type: {voucher.AppliesTo}");
            Console.WriteLine($"Discount Type: {voucher.DiscountType}");
            Console.WriteLine($"Discount Value: {voucher.DiscountValue}");

            if (voucher.AppliesTo == "Order")
            {
                Console.WriteLine("\n--- Order Level Discount ---");
                // Apply discount to entire order
                if (voucher.DiscountType == "PERCENT")
                {
                    discountAmount = cartTotal * (voucher.DiscountValue / 100);
                    Console.WriteLine($"Percentage Discount: {voucher.DiscountValue}%");
                    Console.WriteLine($"Calculated Discount Amount: {discountAmount:C}");
                }
                else if (voucher.DiscountType == "FIXED")
                {
                    discountAmount = Math.Min(voucher.DiscountValue, cartTotal);
                    Console.WriteLine($"Fixed Discount Amount: {voucher.DiscountValue:C}");
                    Console.WriteLine($"Applied Discount (Min of discount and cart total): {discountAmount:C}");
                }
            }
            else if (voucher.AppliesTo == "Product")
            {
                Console.WriteLine("\n--- Product Level Discount ---");
                // Apply discount to specific products only
                var applicableProductIds = voucher.VoucherProducts.Select(vp => vp.ProductID).ToList();
                Console.WriteLine($"Applicable Product IDs: {string.Join(", ", applicableProductIds)}");

                var eligibleCartItems = cart.CartItems.Where(ci => applicableProductIds.Contains(ci.ProductID)).ToList();
                Console.WriteLine($"Number of eligible items in cart: {eligibleCartItems.Count}");

                if (eligibleCartItems.Any())
                {
                    decimal eligibleTotal = eligibleCartItems.Sum(ci => ci.Subtotal);
                    Console.WriteLine($"Total eligible items subtotal: {eligibleTotal:C}");

                    if (voucher.DiscountType == "PERCENT")
                    {
                        discountAmount = eligibleTotal * (voucher.DiscountValue / 100);
                        Console.WriteLine($"Percentage Discount: {voucher.DiscountValue}%");
                        Console.WriteLine($"Calculated Discount Amount: {discountAmount:C}");
                    }
                    else if (voucher.DiscountType == "FIXED")
                    {
                        discountAmount = Math.Min(voucher.DiscountValue, eligibleTotal);
                        Console.WriteLine($"Fixed Discount Amount: {voucher.DiscountValue:C}");
                        Console.WriteLine($"Applied Discount (Min of discount and eligible total): {discountAmount:C}");
                    }
                }
            }

            Console.WriteLine($"\nFinal Discount Amount: {discountAmount:C}");
            Console.WriteLine("=====================================\n");
            return discountAmount;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ClearVoucher()
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy giỏ hàng" });
                }

                HttpContext.Session.Remove("AppliedVoucherId");
                decimal subtotal = cart.CartItems.Sum(ci => ci.Subtotal);
                cart.TotalPrice = subtotal;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đã xóa mã giảm giá",
                    cart = new
                    {
                        totalPrice = subtotal,
                        subtotal = subtotal,
                        discountAmount = 0,
                        cartItemsCount = cart.CartItems.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing voucher for user {Email}", User.Identity.Name);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa mã giảm giá" });
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(int addressId)
        {
            try
            {
                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                var address = await _userService.GetAddressByIdAsync(addressId, user.UserID);
                if (address == null)
                {
                    return Json(new { success = false, message = "Địa chỉ không hợp lệ" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null || !cart.CartItems.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                var appliedVoucherId = HttpContext.Session.GetInt32("AppliedVoucherId");
                Voucher appliedVoucher = null;
                decimal discountAmount = 0;
                decimal subtotal = cart.CartItems.Sum(ci => ci.Subtotal);
                decimal finalPrice = subtotal;

                if (appliedVoucherId.HasValue)
                {
                    appliedVoucher = await _context.Vouchers.FindAsync(appliedVoucherId.Value);
                    if (appliedVoucher != null && IsVoucherValid(appliedVoucher, cart))
                    {
                        discountAmount = CalculateVoucherDiscount(appliedVoucher, cart);
                        finalPrice = subtotal - discountAmount;
                    }
                }

                // Create order
                var order = new Order
                {
                    UserID = user.UserID,
                    TotalPrice = subtotal,
                    DiscountAmount = discountAmount,
                    FinalPrice = finalPrice,
                    Status = "Pending",
                    CreatedAt = DateTime.Now,
                    UserAddressID = addressId
                };

                // Add order items
                foreach (var cartItem in cart.CartItems)
                {
                    Console.WriteLine($"Processing cart item {cartItem.CartItemID} with subtotal {cartItem.Subtotal}");
                    decimal itemDiscountAmount = 0;
                    decimal itemFinalSubtotal = cartItem.Subtotal;

                    // Apply product-specific voucher discount if applicable
                    if (appliedVoucher != null && appliedVoucher.AppliesTo == "Product")
                    {
                        Console.WriteLine($"Voucher {appliedVoucher.Code} applies to Product. DiscountType: {appliedVoucher.DiscountType}");
                        var applicableProductIds = appliedVoucher.VoucherProducts?.Select(vp => vp.ProductID).ToList() ?? new List<int>();

                        if (applicableProductIds.Contains(cartItem.ProductID))
                        {
                            Console.WriteLine($"Cart item {cartItem.CartItemID} is eligible for product-specific voucher.");
                            if (appliedVoucher.DiscountType == "PERCENT")
                            {
                                itemDiscountAmount = cartItem.Subtotal * (appliedVoucher.DiscountValue / 100);
                                itemFinalSubtotal = cartItem.Subtotal - itemDiscountAmount;
                            }
                            else if (appliedVoucher.DiscountType == "FIXED")
                            {
                                // For fixed discounts on multiple products, distribute proportionally
                                decimal eligibleItemsTotal = cart.CartItems
                                    .Where(ci => applicableProductIds.Contains(ci.ProductID))
                                    .Sum(ci => ci.Subtotal);

                                if (eligibleItemsTotal > 0)
                                {
                                    decimal proportion = cartItem.Subtotal / eligibleItemsTotal;
                                    itemDiscountAmount = Math.Min(appliedVoucher.DiscountValue * proportion, cartItem.Subtotal);
                                    itemFinalSubtotal = cartItem.Subtotal - itemDiscountAmount;
                                }
                            }
                            Console.WriteLine($"Product-specific discount for item {cartItem.CartItemID}: DiscountAmount = {itemDiscountAmount}, FinalSubtotal = {itemFinalSubtotal}");
                        }
                    }
                    // For order-level discounts, distribute proportionally
                    else if (appliedVoucher != null && appliedVoucher.AppliesTo == "Order" && subtotal > 0)
                    {
                        Console.WriteLine($"Voucher {appliedVoucher.Code} applies to Order. DiscountType: {appliedVoucher.DiscountType}");
                        decimal proportion = cartItem.Subtotal / subtotal;
                        itemDiscountAmount = discountAmount * proportion;
                        itemFinalSubtotal = cartItem.Subtotal - itemDiscountAmount;
                        Console.WriteLine($"Order-level discount for item {cartItem.CartItemID}: Proportion = {proportion}, DiscountAmount = {itemDiscountAmount}, FinalSubtotal = {itemFinalSubtotal}");
                    }
                    else if (appliedVoucher == null)
                    {
                        Console.WriteLine($"No voucher applied for item {cartItem.CartItemID}.");
                    }

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductID = cartItem.ProductID,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.Product.GetEffectivePrice(),
                        Subtotal = cartItem.Subtotal,
                        DiscountAmount = itemDiscountAmount,
                        FinalSubtotal = itemFinalSubtotal
                    });
                }

                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(cart.CartItems);
                cart.TotalPrice = 0;

                // Update user stats
                user.OrderCount += 1;
                user.TotalSpent += order.FinalPrice;

                // Update voucher quantity
                if (appliedVoucher != null && appliedVoucher.QuantityAvailable.HasValue)
                {
                    appliedVoucher.QuantityAvailable--;
                }

                // Save changes to get the OrderID
                await _context.SaveChangesAsync();

                bool paymentSuccess = new Random().Next(0, 2) == 1;
                if (paymentSuccess)
                {
                    var payment = new Payment
                    {
                        OrderID = order.OrderID,
                        PaymentMethod = "Momo",
                        PaymentStatus = "Completed",
                        Amount = order.FinalPrice,
                        PaymentDate = DateTime.Now
                    };
                    _context.Payments.Add(payment);
                    order.Status = "Processing";
                }
                else
                {
                    var payment = new Payment
                    {
                        OrderID = order.OrderID,
                        PaymentMethod = "Momo",
                        PaymentStatus = "Failed",
                        Amount = order.FinalPrice,
                        PaymentDate = DateTime.Now
                    };
                    _context.Payments.Add(payment);
                    order.Status = "Cancelled";
                }

                HttpContext.Session.Remove("AppliedVoucherId");
                await _context.SaveChangesAsync();

                return Json(new { success = paymentSuccess, message = paymentSuccess ? "Thanh toán thành công" : "Thanh toán thất bại" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout for user {Email}", User.Identity.Name);
                return Json(new { success = false, message = "Có lỗi xảy ra khi thanh toán" });
            }
        }

        private bool IsVoucherValid(Voucher voucher, Cart cart)
        {
            // Check if voucher is active and within valid date range
            if (!voucher.IsActive || voucher.ValidFrom > DateTime.Now || voucher.ValidTo < DateTime.Now)
            {
                return false;
            }

            // Check if voucher has available quantity
            if (voucher.QuantityAvailable.HasValue && voucher.QuantityAvailable <= 0)
            {
                return false;
            }

            // Check if voucher applies to the entire order
            if (voucher.AppliesTo == "Order")
            {
                return true;
            }

            // Check if voucher applies to specific products in the cart
            if (voucher.AppliesTo == "Product" && voucher.VoucherProducts != null)
            {
                var applicableProductIds = voucher.VoucherProducts.Select(vp => vp.ProductID).ToList();
                return cart.CartItems.Any(ci => applicableProductIds.Contains(ci.ProductID));
            }

            return false;
        }
    }
}