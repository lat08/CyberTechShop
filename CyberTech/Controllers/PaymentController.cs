using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CyberTech.Data;
using CyberTech.Models;
using CyberTech.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CyberTech.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly VNPayService _vnPayService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IEmailService _emailService;

        public PaymentController(
            ApplicationDbContext context,
            VNPayService vnPayService,
            ILogger<PaymentController> logger,
            IEmailService emailService)
        {
            _context = context;
            _vnPayService = vnPayService;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessVNPayPayment(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Kiểm tra trạng thái đơn hàng
                if (order.Status == "Cancelled")
                {
                    return Json(new { success = false, message = "Đơn hàng đã bị hủy, không thể thanh toán" });
                }

                // Check if order already has a completed payment
                if (order.Payments.Any(p => p.PaymentStatus == "Completed"))
                {
                    return Json(new { success = false, message = "Đơn hàng đã được thanh toán" });
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var orderInfo = $"Thanh toan don hang {order.OrderID} - {order.User.Email}";
                var paymentUrl = _vnPayService.CreatePaymentUrl(order.OrderID, order.FinalPrice, orderInfo, ipAddress);

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    return Json(new { success = false, message = "Không thể tạo URL thanh toán" });
                }

                return Json(new { success = true, paymentUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay payment for order {OrderId}", orderId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý thanh toán" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> VNPayReturn()
        {
            try
            {
                var responseData = Request.Query.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToString()
                );

                var vnPayResponse = _vnPayService.ProcessVNPayResponse(responseData);

                if (vnPayResponse.Success)
                {
                    var order = await _context.Orders
                        .AsTracking()
                        .Include(o => o.Payments)
                        .Include(o => o.User)
                            .ThenInclude(u => u.Rank)
                        .FirstOrDefaultAsync(o => o.OrderID == vnPayResponse.OrderId);

                    if (order != null)
                    {
                        // Kiểm tra nếu đơn hàng đã bị hủy
                        if (order.Status == "Cancelled")
                        {
                            _logger.LogWarning("Attempted to process payment for cancelled order {OrderId}", order.OrderID);
                            return RedirectToAction("PaymentFailed", new { orderId = order.OrderID, message = "Đơn hàng đã bị hủy, không thể thanh toán" });
                        }

                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            // Update payment status
                            var payment = order.Payments.FirstOrDefault();
                            if (payment == null)
                            {
                                payment = new Payment
                                {
                                    OrderID = order.OrderID,
                                    PaymentMethod = "VNPay",
                                    Amount = vnPayResponse.Amount,
                                    PaymentStatus = "Completed",
                                    PaymentDate = vnPayResponse.PayDate
                                };
                                _context.Payments.Add(payment);
                            }
                            else
                            {
                                payment.PaymentStatus = "Completed";
                                payment.PaymentDate = vnPayResponse.PayDate;
                            }

                            // Update order status
                            order.Status = "Processing";

                            // Update user's TotalSpent and OrderCount
                            if (order.User != null)
                            {
                                var oldRank = order.User.Rank;
                                var oldRankName = oldRank?.RankName ?? "Thành viên";

                                order.User.TotalSpent += order.TotalPrice;
                                order.User.OrderCount++;

                                // Update user's rank if necessary
                                var newRank = await _context.Ranks
                                    .Where(r => r.MinTotalSpent <= order.User.TotalSpent)
                                    .OrderByDescending(r => r.MinTotalSpent)
                                    .FirstOrDefaultAsync();

                                if (newRank != null && order.User.RankId != newRank.RankId)
                                {
                                    order.User.RankId = newRank.RankId;
                                    _logger.LogInformation("User {UserId} rank updated to {RankId} after successful payment", order.User.UserID, newRank.RankId);

                                    // Đảm bảo thay đổi được lưu vào cơ sở dữ liệu
                                    _context.Users.Update(order.User);
                                    _logger.LogInformation("Updating user in database: UserId={UserId}, TotalSpent={TotalSpent}, OrderCount={OrderCount}",
                                        order.User.UserID, order.User.TotalSpent, order.User.OrderCount);

                                    // Send rank upgrade email notification
                                    try
                                    {
                                        await _emailService.SendRankUpgradeEmailAsync(
                                            order.User.Email,
                                            order.User.Name,
                                            oldRankName,
                                            newRank.RankName,
                                            newRank.DiscountPercent ?? 0
                                        );
                                        _logger.LogInformation("Rank upgrade email sent to user {UserId}", order.User.UserID);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error sending rank upgrade email to user {UserId}", order.User.UserID);
                                    }
                                }
                            }

                            try
                            {
                                await _context.SaveChangesAsync();
                                _logger.LogInformation("Changes saved to database successfully");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error saving changes to database");
                                throw;
                            }

                            await transaction.CommitAsync();
                            _logger.LogInformation("Transaction committed successfully");

                            return RedirectToAction("PaymentSuccess", new { orderId = order.OrderID });
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, "Error updating order {OrderId} after successful VNPay payment", order.OrderID);
                            return RedirectToAction("PaymentFailed", new { orderId = order.OrderID });
                        }
                    }
                }

                // If payment failed or order not found
                if (vnPayResponse.OrderId > 0)
                {
                    var order = await _context.Orders
                        .Include(o => o.Payments)
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.Product)
                        .Include(o => o.User)
                        .FirstOrDefaultAsync(o => o.OrderID == vnPayResponse.OrderId);

                    if (order != null)
                    {
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            // Update payment status
                            var payment = order.Payments.FirstOrDefault();
                            if (payment == null)
                            {
                                payment = new Payment
                                {
                                    OrderID = order.OrderID,
                                    PaymentMethod = "VNPay",
                                    Amount = vnPayResponse.Amount,
                                    PaymentStatus = "Failed",
                                    PaymentDate = DateTime.Now
                                };
                                _context.Payments.Add(payment);
                            }
                            else
                            {
                                payment.PaymentStatus = "Failed";
                                payment.PaymentDate = DateTime.Now;
                            }

                            // Update order status
                            order.Status = "Cancelled";

                            // Restore product quantities
                            foreach (var orderItem in order.OrderItems)
                            {
                                var product = orderItem.Product;
                                if (product != null)
                                {
                                    product.Stock += orderItem.Quantity;
                                }
                            }

                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            _logger.LogInformation("Successfully restored product quantities for cancelled order {OrderId}", order.OrderID);
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, "Error updating order {OrderId} after failed VNPay payment", order.OrderID);
                        }
                    }
                }

                return RedirectToAction("PaymentFailed", new { orderId = vnPayResponse.OrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay return");
                return RedirectToAction("PaymentFailed");
            }
        }

        public IActionResult PaymentSuccess(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        public IActionResult PaymentFailed(int? orderId = null, string message = null)
        {
            ViewBag.OrderId = orderId;
            ViewBag.ErrorMessage = message;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessCODPayment([FromQuery] int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .AsTracking()
                    .Include(o => o.User)
                        .ThenInclude(u => u.Rank)
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.OrderID == orderId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Kiểm tra trạng thái đơn hàng
                if (order.Status == "Cancelled")
                {
                    return Json(new { success = false, message = "Đơn hàng đã bị hủy, không thể xử lý" });
                }

                // Check if order already has a completed payment
                if (order.Payments.Any(p => p.PaymentStatus == "Completed"))
                {
                    return Json(new { success = false, message = "Đơn hàng đã được thanh toán" });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Update payment status
                    var payment = order.Payments.FirstOrDefault();
                    if (payment == null)
                    {
                        payment = new Payment
                        {
                            OrderID = order.OrderID,
                            PaymentMethod = "COD",
                            Amount = order.FinalPrice,
                            PaymentStatus = "Pending",
                            PaymentDate = DateTime.Now
                        };
                        _context.Payments.Add(payment);
                    }
                    else
                    {
                        payment.PaymentStatus = "Pending";
                        payment.PaymentDate = DateTime.Now;
                    }

                    // Update order status
                    order.Status = "Processing";

                    // Update user's TotalSpent and OrderCount
                    if (order.User != null)
                    {
                        var oldRank = order.User.Rank;
                        var oldRankName = oldRank?.RankName ?? "Thành viên";

                        order.User.TotalSpent += order.FinalPrice;
                        order.User.OrderCount++;

                        _logger.LogInformation("Updated user {UserId} TotalSpent to {TotalSpent} and OrderCount to {OrderCount} after COD order",
                            order.User.UserID, order.User.TotalSpent, order.User.OrderCount);

                        // Update user's rank if necessary
                        var newRank = await _context.Ranks
                            .Where(r => r.MinTotalSpent <= order.User.TotalSpent)
                            .OrderByDescending(r => r.MinTotalSpent)
                            .FirstOrDefaultAsync();

                        if (newRank != null && order.User.RankId != newRank.RankId)
                        {
                            order.User.RankId = newRank.RankId;
                            _logger.LogInformation("User {UserId} rank updated to {RankId} after COD order", order.User.UserID, newRank.RankId);

                            // Đảm bảo thay đổi được lưu vào cơ sở dữ liệu
                            _context.Users.Update(order.User);
                            _logger.LogInformation("Updating user in database: UserId={UserId}, TotalSpent={TotalSpent}, OrderCount={OrderCount}",
                                order.User.UserID, order.User.TotalSpent, order.User.OrderCount);

                            // Send rank upgrade email notification
                            try
                            {
                                await _emailService.SendRankUpgradeEmailAsync(
                                    order.User.Email,
                                    order.User.Name,
                                    oldRankName,
                                    newRank.RankName,
                                    newRank.DiscountPercent ?? 0
                                );
                                _logger.LogInformation("Rank upgrade email sent to user {UserId}", order.User.UserID);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error sending rank upgrade email to user {UserId}", order.User.UserID);
                            }
                        }
                    }

                    try
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Changes saved to database successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving changes to database");
                        throw;
                    }

                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction committed successfully");

                    return Json(new { success = true, message = "Đơn hàng COD đã được xử lý thành công" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error processing COD payment for order {OrderId}", orderId);
                    return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý đơn hàng COD" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing COD payment for order {OrderId}", orderId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý đơn hàng COD" });
            }
        }
    }
}