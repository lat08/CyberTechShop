using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CyberTech.Models;
using CyberTech.ViewModels;
using CyberTech.Services;
using Microsoft.EntityFrameworkCore;
using CyberTech.Data;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CyberTech.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ApplicationDbContext context,
            IUserService userService,
            ILogger<CartController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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

                _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    _logger.LogError("User not found for email: {Email}", emailClaim.Value);
                    return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
                }

                _logger.LogInformation("User found: UserID={UserID}, Email={Email}", user.UserID, user.Email);

                // Get or create cart
                var cart = await GetOrCreateCartAsync(user.UserID);
                if (cart == null)
                {
                    _logger.LogError("Failed to get or create cart for user: {UserId}", user.UserID);
                    return View(new CartViewModel()); // Return empty cart view
                }

                _logger.LogInformation("Cart query result: {Cart}", cart != null ?
                    $"CartID={cart.CartID}, UserID={cart.UserID}, TotalPrice={cart.TotalPrice}, Items={cart.CartItems?.Count ?? 0}" :
                    "Cart is null");

                // Load cart items with related data
                var cartItems = await LoadCartItemsAsync(cart.CartID);

                var viewModel = new CartViewModel
                {
                    CartItems = cartItems.Select(ci => new CartItemViewModel
                    {
                        ProductID = ci.ProductID,
                        ProductName = ci.Product?.Name ?? "Unknown Product",
                        Quantity = ci.Quantity,
                        Subtotal = ci.Subtotal,
                        // ProductImage = ci.Product?.ProductImages?.FirstOrDefault()?.ImageURL ?? "/images/no-image.png"
                    }).ToList(),
                    TotalPrice = cart.TotalPrice
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Cart Index action");
                return View(new CartViewModel()); // Return empty cart view on error
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                if (productId <= 0 || quantity <= 0)
                {
                    return Json(new { success = false, message = "Invalid product or quantity" });
                }

                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Unauthorized("Invalid user identifier.");
                }

                _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    _logger.LogError("User not found for email: {Email}", emailClaim.Value);
                    return Json(new { success = false, message = "User not found" });
                }

                _logger.LogInformation("User found: UserID={UserID}, Email={Email}", user.UserID, user.Email);

                // Get or create cart
                var cart = await GetOrCreateCartAsync(user.UserID);
                if (cart == null)
                {
                    return Json(new { success = false, message = "Failed to get or create cart" });
                }

                // Verify product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Update or add cart item
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartID == cart.CartID && ci.ProductID == productId);

                if (cartItem != null)
                {
                    cartItem.Quantity += quantity;
                    cartItem.Subtotal = cartItem.Quantity * product.Price;
                }
                else
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

                // Update cart total
                cart.TotalPrice = await _context.CartItems
                    .Where(ci => ci.CartID == cart.CartID)
                    .SumAsync(ci => ci.Subtotal);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Added to cart successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart", productId);
                return Json(new { success = false, message = "An error occurred while adding to cart" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateOrder()
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
                if (user == null)
                {
                    _logger.LogError("User not found for email: {Email}", emailClaim.Value);
                    return Json(new { success = false, message = "User not found" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserID == user.UserID);

                if (cart == null || !cart.CartItems.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng của bạn đang trống" });
                }

                // Check stock
                foreach (var item in cart.CartItems)
                {
                    if (item.Quantity > item.Product.Stock)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Sản phẩm '{item.Product.Name}' chỉ còn {item.Product.Stock} trong kho"
                        });
                    }
                }

                // Calculate totals
                decimal totalPrice = cart.TotalPrice;
                decimal totalDiscount = 0;

                // Calculate user rank discount if exists

                decimal finalPrice = totalPrice - totalDiscount;

                var order = new Order
                {
                    UserID = user.UserID,
                    TotalPrice = totalPrice,
                    DiscountAmount = totalDiscount,
                    FinalPrice = finalPrice,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                foreach (var item in cart.CartItems)
                {
                    // Calculate item totals
                    decimal itemSubtotal = item.Subtotal;
                    decimal itemDiscount = 0;

                    // Calculate item discount based on user rank

                    decimal itemFinalSubtotal = itemSubtotal - itemDiscount;

                    var orderItem = new OrderItem
                    {
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        Subtotal = itemSubtotal,
                        DiscountAmount = itemDiscount,
                        FinalSubtotal = itemFinalSubtotal,
                        CreatedAt = DateTime.Now
                    };
                    order.OrderItems.Add(orderItem);

                    // Update stock
                    item.Product.Stock -= item.Quantity;
                }

                _context.Orders.Add(order);

                // Clear cart items
                _context.CartItems.RemoveRange(cart.CartItems);
                cart.TotalPrice = 0;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đặt hàng thành công",
                    orderId = order.OrderID,
                    redirectUrl = Url.Action("OrderDetails", "Account", new { id = order.OrderID })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for user: {Email}", User.FindFirst(ClaimTypes.Email)?.Value);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo đơn hàng" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            try
            {
                if (productId <= 0 || quantity <= 0)
                {
                    return Json(new { success = false, message = "Invalid product or quantity" });
                }

                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Unauthorized("Invalid user identifier.");
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .Include(ci => ci.Product)
                    .FirstOrDefaultAsync(ci =>
                        ci.ProductID == productId &&
                        ci.Cart.UserID == user.UserID);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Cart item not found" });
                }

                cartItem.Quantity = quantity;
                cartItem.Subtotal = cartItem.Product.Price * quantity;

                // Update cart total
                cartItem.Cart.TotalPrice = await _context.CartItems
                    .Where(ci => ci.CartID == cartItem.CartID)
                    .SumAsync(ci => ci.Subtotal);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    subtotal = cartItem.Subtotal,
                    totalPrice = cartItem.Cart.TotalPrice
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity for product {ProductId}", productId);
                return Json(new { success = false, message = "An error occurred while updating quantity" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            try
            {
                if (productId <= 0)
                {
                    return Json(new { success = false, message = "Invalid product ID" });
                }

                var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                if (emailClaim == null)
                {
                    _logger.LogError("Email claim not found in user claims");
                    return Unauthorized("Invalid user identifier.");
                }

                var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Cart)
                    .FirstOrDefaultAsync(ci =>
                        ci.ProductID == productId &&
                        ci.Cart.UserID == user.UserID);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Cart item not found" });
                }

                _context.CartItems.Remove(cartItem);

                // Update cart total
                cartItem.Cart.TotalPrice = await _context.CartItems
                    .Where(ci => ci.CartID == cartItem.CartID)
                    .SumAsync(ci => ci.Subtotal);

                await _context.SaveChangesAsync();

                return Json(new { success = true, totalPrice = cartItem.Cart.TotalPrice });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product {ProductId} from cart", productId);
                return Json(new { success = false, message = "An error occurred while removing item" });
            }
        }

        // Helper methods
        private async Task<Cart> GetOrCreateCartAsync(int userId)
        {
            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserID == userId);

            if (cart == null)
            {
                cart = new Cart { UserID = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        private async Task<List<CartItem>> LoadCartItemsAsync(int cartId)
        {
            return await _context.CartItems
                .Include(ci => ci.Product)
                    .ThenInclude(p => p.ProductImages)
                .Where(ci => ci.CartID == cartId)
                .ToListAsync();
        }
    }
}