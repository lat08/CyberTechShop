using CyberTech.Data;
using CyberTech.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CyberTech.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartService> _logger;

        public CartService(ApplicationDbContext context, ILogger<CartService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Cart> GetCartAsync(int userId)
        {
            try
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.ProductImages)
                    .FirstOrDefaultAsync(c => c.UserID == userId);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserID = userId,
                        TotalPrice = 0,
                        CartItems = new List<CartItem>()
                    };

                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                return cart;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for user {UserId}", userId);
                return null;
            }
        }

        public async Task<CartItem> GetCartItemAsync(int cartId, int productId)
        {
            try
            {
                return await _context.CartItems
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci => ci.CartID == cartId && ci.ProductID == productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item for cart {CartId} and product {ProductId}", cartId, productId);
                return null;
            }
        }

        public async Task<bool> AddToCartAsync(int userId, int productId, int quantity)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                if (cart == null) return false;

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductID == productId && p.Status == "Active");

                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found or not active", productId);
                    return false;
                }

                if (product.Stock < quantity)
                {
                    _logger.LogWarning("Not enough stock for product {ProductId}. Available: {Stock}, Requested: {Quantity}",
                        productId, product.Stock, quantity);
                    return false;
                }

                var cartItem = await GetCartItemAsync(cart.CartID, productId);

                if (cartItem == null)
                {
                    cartItem = new CartItem
                    {
                        CartID = cart.CartID,
                        ProductID = productId,
                        Quantity = quantity,
                        Subtotal = product.Price * quantity
                    };

                    _context.CartItems.Add(cartItem);
                }
                else
                {
                    if (product.Stock < cartItem.Quantity + quantity)
                    {
                        _logger.LogWarning("Not enough stock for product {ProductId}. Available: {Stock}, Requested: {Quantity}",
                            productId, product.Stock, cartItem.Quantity + quantity);
                        return false;
                    }

                    cartItem.Quantity += quantity;
                    cartItem.Subtotal = product.Price * cartItem.Quantity;
                }

                // Update cart total price (original prices)
                cart.TotalPrice = await CalculateCartTotalAsync(cart.CartID);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart for user {UserId}", productId, userId);
                return false;
            }
        }

        public async Task<bool> UpdateCartItemAsync(int userId, int productId, int quantity)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                if (cart == null) return false;

                var cartItem = await GetCartItemAsync(cart.CartID, productId);
                if (cartItem == null) return false;

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductID == productId && p.Status == "Active");

                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found or not active", productId);
                    return false;
                }

                if (quantity <= 0)
                {
                    return await RemoveFromCartAsync(userId, productId);
                }

                if (product.Stock < quantity)
                {
                    _logger.LogWarning("Not enough stock for product {ProductId}. Available: {Stock}, Requested: {Quantity}",
                        productId, product.Stock, quantity);
                    return false;
                }

                cartItem.Quantity = quantity;
                cartItem.Subtotal = product.Price * quantity;

                // Update cart total price (original prices)
                cart.TotalPrice = await CalculateCartTotalAsync(cart.CartID);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item for user {UserId} and product {ProductId}", userId, productId);
                return false;
            }
        }

        public async Task<bool> RemoveFromCartAsync(int userId, int productId)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                if (cart == null) return false;

                var cartItem = await GetCartItemAsync(cart.CartID, productId);
                if (cartItem == null) return false;

                _context.CartItems.Remove(cartItem);

                // Update cart total price (original prices)
                cart.TotalPrice = await CalculateCartTotalAsync(cart.CartID);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product {ProductId} from cart for user {UserId}", productId, userId);
                return false;
            }
        }

        public async Task<bool> ClearCartAsync(int userId)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                if (cart == null) return false;

                var cartItems = await _context.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .ToListAsync();

                _context.CartItems.RemoveRange(cartItems);
                cart.TotalPrice = 0;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
                return false;
            }
        }

        public async Task<int> GetCartItemCountAsync(int userId)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                if (cart == null) return 0;

                return await _context.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .SumAsync(ci => ci.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<decimal> GetCartTotalAsync(int userId)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                if (cart == null) return 0;

                return await CalculateCartTotalAsync(cart.CartID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart total for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<List<CartItem>> GetCartItemsAsync(int userId)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                if (cart == null) return new List<CartItem>();

                return await _context.CartItems
                    .Include(ci => ci.Product)
                        .ThenInclude(p => p.ProductImages)
                    .Where(ci => ci.CartID == cart.CartID)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart items for user {UserId}", userId);
                return new List<CartItem>();
            }
        }

        public async Task<(bool Success, decimal DiscountAmount, string Message)> ApplyVoucherAsync(int userId, string voucherCode)
        {
            try
            {
                if (string.IsNullOrEmpty(voucherCode))
                {
                    return (false, 0, "Mã giảm giá không được để trống");
                }

                var voucher = await _context.Vouchers
                    .Include(v => v.VoucherProducts)
                    .FirstOrDefaultAsync(v => v.Code == voucherCode && v.IsActive);

                if (voucher == null)
                {
                    return (false, 0, "Mã giảm giá không tồn tại hoặc đã hết hạn");
                }

                if (voucher.ValidFrom > DateTime.Now || voucher.ValidTo < DateTime.Now)
                {
                    return (false, 0, "Mã giảm giá không trong thời gian sử dụng");
                }

                if (voucher.QuantityAvailable.HasValue && voucher.QuantityAvailable <= 0)
                {
                    return (false, 0, "Mã giảm giá đã hết lượt sử dụng");
                }

                var cart = await GetCartAsync(userId);
                if (cart == null || !cart.CartItems.Any())
                {
                    return (false, 0, "Giỏ hàng trống");
                }

                // Get user's rank for discount calculation
                var user = await _context.Users
                    .Include(u => u.Rank)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                decimal rankDiscountPercent = user?.Rank?.DiscountPercent ?? 0;

                // Calculate original price total
                decimal originalTotal = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity);

                // Calculate product discounts (from sales)
                decimal productDiscountAmount = 0;
                foreach (var item in cart.CartItems)
                {
                    var product = item.Product;
                    var originalPrice = product.Price * item.Quantity;
                    var effectivePrice = product.GetEffectivePrice() * item.Quantity;
                    productDiscountAmount += originalPrice - effectivePrice;
                }

                // Calculate amount after product discount
                decimal amountAfterProductDiscount = originalTotal - productDiscountAmount;

                // Calculate rank discount
                decimal rankDiscountAmount = amountAfterProductDiscount * (rankDiscountPercent / 100);

                // Calculate amount after rank discount
                decimal amountAfterRankDiscount = amountAfterProductDiscount - rankDiscountAmount;

                // Now calculate voucher discount
                decimal voucherDiscountAmount = 0;

                // Apply voucher based on type
                if (voucher.AppliesTo == "Order")
                {
                    // Apply to entire order after product and rank discounts
                    if (voucher.DiscountType == "PERCENT")
                    {
                        voucherDiscountAmount = amountAfterRankDiscount * (voucher.DiscountValue / 100);
                    }
                    else if (voucher.DiscountType == "FIXED")
                    {
                        voucherDiscountAmount = Math.Min(voucher.DiscountValue, amountAfterRankDiscount);
                    }
                }
                else if (voucher.AppliesTo == "Product")
                {
                    // Apply to specific products only
                    var voucherProductIds = voucher.VoucherProducts.Select(vp => vp.ProductID).ToList();
                    var eligibleCartItems = cart.CartItems.Where(ci => voucherProductIds.Contains(ci.ProductID)).ToList();

                    if (!eligibleCartItems.Any())
                    {
                        return (false, 0, "Mã giảm giá không áp dụng cho sản phẩm nào trong giỏ hàng");
                    }

                    // Calculate eligible total after product and rank discounts
                    decimal eligibleTotal = 0;
                    foreach (var item in eligibleCartItems)
                    {
                        var product = item.Product;
                        var originalItemPrice = product.Price * item.Quantity;
                        var effectivePrice = product.GetEffectivePrice() * item.Quantity;
                        var itemProductDiscount = originalItemPrice - effectivePrice;
                        var itemAfterProductDiscount = originalItemPrice - itemProductDiscount;
                        var itemRankDiscount = itemAfterProductDiscount * (rankDiscountPercent / 100);
                        var itemAfterAllDiscounts = itemAfterProductDiscount - itemRankDiscount;
                        eligibleTotal += itemAfterAllDiscounts;
                    }

                    if (voucher.DiscountType == "PERCENT")
                    {
                        voucherDiscountAmount = eligibleTotal * (voucher.DiscountValue / 100);
                    }
                    else if (voucher.DiscountType == "FIXED")
                    {
                        voucherDiscountAmount = Math.Min(voucher.DiscountValue, eligibleTotal);
                    }
                }

                // Ensure voucher discount doesn't exceed the remaining amount
                voucherDiscountAmount = Math.Min(voucherDiscountAmount, amountAfterRankDiscount);

                // Store voucher info in session (will be implemented in controller)
                if (voucher.QuantityAvailable.HasValue)
                {
                    voucher.QuantityAvailable--;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Successfully applied voucher {VoucherCode}. Discount amount: {DiscountAmount}",
                    voucher.Code, voucherDiscountAmount);

                return (true, voucherDiscountAmount, "Áp dụng mã giảm giá thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying voucher {VoucherCode} for user {UserId}", voucherCode, userId);
                return (false, 0, "Có lỗi xảy ra khi áp dụng mã giảm giá");
            }
        }

        public async Task<bool> RemoveVoucherAsync(int userId)
        {
            // This will be implemented in the controller by removing voucher from session
            return await Task.FromResult(true);
        }

        public async Task<(bool Success, string Message, int? OrderId)> CheckoutAsync(int userId, int addressId, string paymentMethod, int? voucherId = null)
        {
            try
            {
                // Validate payment method
                if (paymentMethod != "COD" && paymentMethod != "VNPay")
                {
                    return (false, "Phương thức thanh toán không hợp lệ", null);
                }

                var cart = await GetCartAsync(userId);
                if (cart == null || !cart.CartItems.Any())
                {
                    return (false, "Giỏ hàng trống", null);
                }

                var address = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.AddressID == addressId && a.UserID == userId);

                if (address == null)
                {
                    return (false, "Địa chỉ giao hàng không hợp lệ", null);
                }

                // Get user's rank for discount calculation
                var user = await _context.Users
                    .Include(u => u.Rank)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                // Start transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Calculate original price total (without any discounts)
                    decimal originalTotal = cart.CartItems.Sum(ci => ci.Product.Price * ci.Quantity);

                    // Calculate product discounts (from sales)
                    decimal productDiscountAmount = 0;
                    foreach (var cartItem in cart.CartItems)
                    {
                        var product = await _context.Products.FindAsync(cartItem.ProductID);
                        if (product == null || product.Stock < cartItem.Quantity)
                        {
                            await transaction.RollbackAsync();
                            return (false, $"Sản phẩm {product?.Name ?? "không xác định"} không đủ số lượng", null);
                        }

                        // Calculate product discount if on sale
                        if (product.SalePrice.HasValue && product.SalePrice < product.Price)
                        {
                            productDiscountAmount += (product.Price - product.SalePrice.Value) * cartItem.Quantity;
                        }
                        else if (product.SalePercentage.HasValue)
                        {
                            decimal discountedPrice = product.Price * (1 - (product.SalePercentage.Value / 100));
                            productDiscountAmount += (product.Price - discountedPrice) * cartItem.Quantity;
                        }

                        // Update product stock
                        product.Stock -= cartItem.Quantity;
                    }

                    // Calculate amount after product discount
                    decimal amountAfterProductDiscount = originalTotal - productDiscountAmount;

                    // Calculate rank discount if applicable
                    decimal rankDiscountAmount = 0;
                    if (user?.Rank != null && user.Rank.DiscountPercent.HasValue)
                    {
                        rankDiscountAmount = amountAfterProductDiscount * (user.Rank.DiscountPercent.Value / 100);
                    }

                    // Calculate voucher discount if applicable
                    decimal voucherDiscountAmount = 0;
                    if (voucherId.HasValue)
                    {
                        var voucher = await _context.Vouchers
                            .Include(v => v.VoucherProducts)
                            .FirstOrDefaultAsync(v => v.VoucherID == voucherId.Value && v.IsActive);

                        if (voucher != null)
                        {
                            // Lấy mã voucher
                            string voucherCode = voucher.Code;

                            // Áp dụng voucher
                            var (success, discountAmount, _) = await ApplyVoucherAsync(userId, voucherCode);
                            if (success)
                            {
                                voucherDiscountAmount = discountAmount;
                                _logger.LogInformation("Applied voucher {VoucherCode} with discount amount {DiscountAmount} for order",
                                    voucherCode, voucherDiscountAmount);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to apply voucher {VoucherCode} for order", voucherCode);
                            }
                        }
                    }

                    // Calculate total discount and final price
                    decimal totalDiscountAmount = productDiscountAmount + rankDiscountAmount + voucherDiscountAmount;
                    decimal finalPrice = originalTotal - totalDiscountAmount;

                    // Create order with all discount details
                    var order = new Order
                    {
                        UserID = userId,
                        TotalPrice = originalTotal,  // Original price without any discounts
                        ProductDiscountAmount = productDiscountAmount,
                        RankDiscountAmount = rankDiscountAmount,
                        VoucherDiscountAmount = voucherDiscountAmount,
                        TotalDiscountAmount = totalDiscountAmount,
                        FinalPrice = finalPrice,
                        Status = "Pending",
                        CreatedAt = DateTime.Now,
                        UserAddressID = addressId
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Create order items with original and effective prices
                    foreach (var cartItem in cart.CartItems)
                    {
                        var product = await _context.Products.FindAsync(cartItem.ProductID);
                        if (product == null || product.Stock < cartItem.Quantity)
                        {
                            await transaction.RollbackAsync();
                            return (false, $"Sản phẩm {product?.Name ?? "không xác định"} không đủ số lượng", null);
                        }

                        // Calculate item's effective price after product discount
                        decimal effectiveUnitPrice = product.GetEffectivePrice();
                        decimal originalSubtotal = product.Price * cartItem.Quantity;
                        decimal effectiveSubtotal = effectiveUnitPrice * cartItem.Quantity;
                        decimal productDiscount = originalSubtotal - effectiveSubtotal;

                        // Calculate item discount based on rank
                        decimal itemRankDiscount = 0;
                        if (user?.Rank?.DiscountPercent.HasValue == true)
                        {
                            itemRankDiscount = effectiveSubtotal * (user.Rank.DiscountPercent.Value / 100);
                        }

                        // Create order item with all price details
                        var orderItem = new OrderItem
                        {
                            OrderID = order.OrderID,
                            ProductID = cartItem.ProductID,
                            Quantity = cartItem.Quantity,
                            UnitPrice = product.Price,  // Original price
                            Subtotal = originalSubtotal,  // Original subtotal
                            DiscountAmount = productDiscount + itemRankDiscount,  // Total discount
                            FinalSubtotal = effectiveSubtotal - itemRankDiscount  // Final price after all discounts
                        };

                        _context.OrderItems.Add(orderItem);
                    }

                    // Create payment
                    var payment = new Payment
                    {
                        OrderID = order.OrderID,
                        PaymentMethod = paymentMethod,
                        PaymentStatus = paymentMethod == "COD" ? "Pending" : "Pending",
                        Amount = order.FinalPrice,
                        PaymentDate = DateTime.Now
                    };

                    _context.Payments.Add(payment);

                    // Update user stats
                    if (user != null && paymentMethod == "COD")
                    {
                        user.OrderCount++;
                        user.TotalSpent += order.FinalPrice;

                        // Check if user qualifies for rank upgrade
                        var nextRank = await _context.Ranks
                            .Where(r => r.MinTotalSpent <= user.TotalSpent)
                            .OrderByDescending(r => r.MinTotalSpent)
                            .FirstOrDefaultAsync();

                        if (nextRank != null && (user.RankId == null || user.RankId < nextRank.RankId))
                        {
                            user.RankId = nextRank.RankId;
                        }
                    }

                    // Clear cart
                    await ClearCartAsync(userId);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, "Đặt hàng thành công", order.OrderID);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during checkout for user {UserId}", userId);
                    return (false, "Có lỗi xảy ra trong quá trình đặt hàng", null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout for user {UserId}", userId);
                return (false, "Có lỗi xảy ra trong quá trình đặt hàng", null);
            }
        }

        // Add method to update payment status
        public async Task<(bool Success, string Message)> UpdatePaymentStatusAsync(int orderId, string paymentStatus)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

                if (order == null)
                {
                    return (false, "Không tìm thấy đơn hàng");
                }

                var payment = order.Payments.FirstOrDefault();
                if (payment == null)
                {
                    return (false, "Không tìm thấy thông tin thanh toán");
                }

                // Validate payment status
                if (!new[] { "Pending", "Completed", "Failed", "Refunded" }.Contains(paymentStatus))
                {
                    return (false, "Trạng thái thanh toán không hợp lệ");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Update payment status
                    payment.PaymentStatus = paymentStatus;
                    payment.PaymentDate = DateTime.Now;

                    // Update order status based on payment status
                    switch (paymentStatus)
                    {
                        case "Completed":
                            order.Status = "Processing";
                            break;
                        case "Failed":
                            order.Status = "Cancelled";
                            break;
                        case "Refunded":
                            order.Status = "Cancelled";
                            break;
                        default:
                            order.Status = "Pending";
                            break;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return (true, "Cập nhật trạng thái thanh toán thành công");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating payment status for order {OrderId}", orderId);
                    return (false, "Có lỗi xảy ra khi cập nhật trạng thái thanh toán");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for order {OrderId}", orderId);
                return (false, "Có lỗi xảy ra khi cập nhật trạng thái thanh toán");
            }
        }

        private async Task<decimal> CalculateCartTotalAsync(int cartId)
        {
            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CartID == cartId)
                .ToListAsync();

            decimal total = 0;
            foreach (var item in cartItems)
            {
                // Use original price instead of effective price
                total += item.Product.Price * item.Quantity;
            }
            return total;
        }
    }
}