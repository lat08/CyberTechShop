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
                var price = product.SalePrice ?? product.Price;

                if (cartItem == null)
                {
                    cartItem = new CartItem
                    {
                        CartID = cart.CartID,
                        ProductID = productId,
                        Quantity = quantity,
                        Subtotal = price * quantity
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
                    cartItem.Subtotal = price * cartItem.Quantity;
                }

                // Update cart total
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

                var price = product.SalePrice ?? product.Price;
                cartItem.Quantity = quantity;
                cartItem.Subtotal = price * quantity;

                // Update cart total
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

                // Update cart total
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

                decimal discountAmount = 0;

                // Apply voucher based on type
                if (voucher.AppliesTo == "Order")
                {
                    // Apply to entire order
                    if (voucher.DiscountType == "PERCENT")
                    {
                        discountAmount = cart.TotalPrice * (voucher.DiscountValue / 100);
                    }
                    else if (voucher.DiscountType == "FIXED")
                    {
                        discountAmount = Math.Min(voucher.DiscountValue, cart.TotalPrice);
                    }
                }
                else if (voucher.AppliesTo == "Product")
                {
                    // Apply to specific products
                    var voucherProductIds = voucher.VoucherProducts.Select(vp => vp.ProductID).ToList();
                    var eligibleCartItems = cart.CartItems.Where(ci => voucherProductIds.Contains(ci.ProductID)).ToList();

                    if (!eligibleCartItems.Any())
                    {
                        return (false, 0, "Mã giảm giá không áp dụng cho sản phẩm nào trong giỏ hàng");
                    }

                    decimal eligibleTotal = eligibleCartItems.Sum(ci => ci.Subtotal);

                    if (voucher.DiscountType == "PERCENT")
                    {
                        discountAmount = eligibleTotal * (voucher.DiscountValue / 100);
                    }
                    else if (voucher.DiscountType == "FIXED")
                    {
                        discountAmount = Math.Min(voucher.DiscountValue, eligibleTotal);
                    }
                }

                // Store voucher info in session (will be implemented in controller)
                if (voucher.QuantityAvailable.HasValue)
                {
                    voucher.QuantityAvailable--;
                    await _context.SaveChangesAsync();
                }

                return (true, discountAmount, "Áp dụng mã giảm giá thành công");
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

        public async Task<(bool Success, string Message)> CheckoutAsync(int userId, int addressId, string paymentMethod)
        {
            try
            {
                var cart = await GetCartAsync(userId);
                if (cart == null || !cart.CartItems.Any())
                {
                    return (false, "Giỏ hàng trống");
                }

                var address = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.AddressID == addressId && a.UserID == userId);

                if (address == null)
                {
                    return (false, "Địa chỉ giao hàng không hợp lệ");
                }

                // Start transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create order
                    var order = new Order
                    {
                        UserID = userId,
                        TotalPrice = cart.TotalPrice,
                        DiscountAmount = 0, // Will be set from session in controller
                        FinalPrice = cart.TotalPrice, // Will be adjusted in controller
                        Status = "Pending",
                        CreatedAt = DateTime.Now
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Create order items
                    foreach (var cartItem in cart.CartItems)
                    {
                        var product = await _context.Products.FindAsync(cartItem.ProductID);
                        if (product == null || product.Stock < cartItem.Quantity)
                        {
                            await transaction.RollbackAsync();
                            return (false, $"Sản phẩm {product?.Name ?? "không xác định"} không đủ số lượng");
                        }

                        // Update product stock
                        product.Stock -= cartItem.Quantity;

                        // Create order item
                        var orderItem = new OrderItem
                        {
                            OrderID = order.OrderID,
                            ProductID = cartItem.ProductID,
                            Quantity = cartItem.Quantity,
                            UnitPrice = product.SalePrice ?? product.Price,
                            Subtotal = cartItem.Subtotal,
                            DiscountAmount = 0, // Will be set if applicable
                            FinalSubtotal = cartItem.Subtotal // Will be adjusted if applicable
                        };

                        _context.OrderItems.Add(orderItem);
                    }

                    // Create payment
                    var payment = new Payment
                    {
                        OrderID = order.OrderID,
                        PaymentMethod = paymentMethod,
                        PaymentStatus = paymentMethod == "COD" ? "Pending" : "Completed",
                        Amount = order.FinalPrice,
                        PaymentDate = paymentMethod == "COD" ? (DateTime?)null : DateTime.Now
                    };

                    _context.Payments.Add(payment);

                    // Update user stats
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
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

                    return (true, "Đặt hàng thành công");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during checkout for user {UserId}", userId);
                    return (false, "Có lỗi xảy ra trong quá trình đặt hàng");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout for user {UserId}", userId);
                return (false, "Có lỗi xảy ra trong quá trình đặt hàng");
            }
        }

        private async Task<decimal> CalculateCartTotalAsync(int cartId)
        {
            return await _context.CartItems
                .Where(ci => ci.CartID == cartId)
                .SumAsync(ci => ci.Subtotal);
        }
    }
}