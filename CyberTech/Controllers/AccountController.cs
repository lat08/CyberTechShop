using CyberTech.Models;
using CyberTech.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.Configuration;
using CyberTech.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using BCrypt.Net;

namespace CyberTech.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IRecaptchaService _recaptchaService;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AccountController(
            IUserService userService,
            IRecaptchaService recaptchaService,
            ILogger<AccountController> logger,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userService = userService;
            _recaptchaService = recaptchaService;
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["RecaptchaSiteKey"] = _configuration["Recaptcha:SiteKey"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string recaptchaResponse, string returnUrl = null)
        {
            try
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var (success, errorMessage, user) = await _userService.AuthenticateAsync(email, password);
                    if (!success) return Json(new { success = false, errorMessage = errorMessage ?? "Email hoặc mật khẩu không đúng" });

                    await SignInUserAsync(user, rememberMe);
                    return Json(new { success = true, returnUrl = returnUrl ?? Url.Action("Index", "Home") });
                }

                var (successNonAjax, errorMessageNonAjax, userNonAjax) = await _userService.AuthenticateAsync(email, password);
                if (!successNonAjax)
                {
                    ModelState.AddModelError("", errorMessageNonAjax ?? "Email hoặc mật khẩu không đúng");
                    return View();
                }

                await SignInUserAsync(userNonAjax, rememberMe);
                return Redirect(returnUrl ?? Url.Action("Index", "Home"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", email);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, errorMessage = "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại sau." });
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại sau.");
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLogin(string provider, string returnUrl = null)
        {
            try
            {
                if (string.IsNullOrEmpty(provider) || !new[] { "Google", "Facebook" }.Contains(provider))
                {
                    _logger.LogWarning("Invalid provider specified: {Provider}", provider);
                    return RedirectToAction("Login");
                }

                var properties = new AuthenticationProperties
                {
                    RedirectUri = Url.Action("ExternalLoginCallback", "Account", new { returnUrl }),
                    Items = { { "LoginProvider", provider } }
                };

                return Challenge(properties, provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during external login initiation");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi bắt đầu đăng nhập");
                return View("Login");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            try
            {
                if (remoteError != null)
                {
                    _logger.LogWarning("External login failed with remote error: {RemoteError}", remoteError);
                    ModelState.AddModelError("", $"Lỗi từ nhà cung cấp: {remoteError}");
                    return RedirectToAction("Login");
                }

                var info = await HttpContext.AuthenticateAsync();
                if (info == null || !info.Succeeded)
                {
                    _logger.LogError("External authentication failed");
                    ModelState.AddModelError("", "Lỗi xác thực với nhà cung cấp");
                    return RedirectToAction("Login");
                }

                var provider = info.Properties?.Items["LoginProvider"];
                if (string.IsNullOrEmpty(provider) || !new[] { "Google", "Facebook" }.Contains(provider))
                {
                    _logger.LogError("No valid provider specified in authentication properties. Provider: {Provider}", provider);
                    ModelState.AddModelError("", "Không thể xác định nhà cung cấp đăng nhập");
                    return RedirectToAction("Login");
                }

                var providerId = info.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = info.Principal.FindFirst(ClaimTypes.Email)?.Value;
                var name = info.Principal.FindFirst(ClaimTypes.Name)?.Value;

                string picture = null;
                if (provider == "Google") picture = info.Principal.FindFirst("picture")?.Value;
                else if (provider == "Facebook")
                {
                    var pictureClaim = info.Principal.FindFirst("picture");
                    if (pictureClaim != null)
                    {
                        try
                        {
                            var pictureData = JsonDocument.Parse(pictureClaim.Value);
                            picture = pictureData.RootElement.GetProperty("data").GetProperty("url").GetString();
                        }
                        catch
                        {
                            _logger.LogWarning("Could not parse Facebook picture data for user {Email}", email);
                        }
                    }
                }

                _logger.LogInformation("External login info: Provider={Provider}, Email={Email}, Name={Name}, Picture={Picture}", provider, email, name, picture);

                if (string.IsNullOrEmpty(providerId) || string.IsNullOrEmpty(email))
                {
                    _logger.LogError("Missing required claims: ProviderId={ProviderId}, Email={Email}", providerId, email);
                    ModelState.AddModelError("", "Không thể lấy thông tin từ nhà cung cấp");
                    return RedirectToAction("Login");
                }

                var existingUser = await _userService.GetUserByEmailAsync(email);
                if (existingUser != null)
                {
                    if (existingUser.UserStatus != "Active")
                    {
                        _logger.LogWarning("User attempted to login with external provider but account is {Status}: {Email}", existingUser.UserStatus, email);
                        ModelState.AddModelError("", $"Tài khoản của bạn hiện đang {(existingUser.UserStatus == "Suspended" ? "bị khóa" : "không hoạt động")}"); // Changed from UserStatus.Suspended
                        TempData["ErrorMessage"] = $"Tài khoản của bạn hiện đang {(existingUser.UserStatus == "Suspended" ? "bị khóa" : "không hoạt động")}"; // Changed from UserStatus.Suspended
                        return RedirectToAction("Login");
                    }

                    var existingAuthMethod = await _userService.GetUserAuthMethodAsync(existingUser.UserID, provider);
                    if (existingAuthMethod == null)
                    {
                        _logger.LogInformation("User {Email} exists but doesn't have {Provider} auth method. Adding it now.", email, provider);
                        var authMethodSuccess = await _userService.AddAuthMethodAsync(existingUser.UserID, provider, providerId);
                        if (!authMethodSuccess)
                        {
                            _logger.LogWarning("Failed to add {Provider} auth method for user {Email}", provider, email);
                            ModelState.AddModelError("", $"Không thể thêm phương thức đăng nhập {provider} cho tài khoản hiện có");
                            TempData["ErrorMessage"] = $"Không thể thêm phương thức đăng nhập {provider} cho tài khoản hiện có";
                            return RedirectToAction("Login");
                        }
                    }
                }

                var (success, errorMessage, user) = await _userService.ExternalLoginAsync(provider, providerId, email, name ?? "Unknown");
                if (!success || user == null)
                {
                    _logger.LogWarning("External login processing failed: {ErrorMessage}", errorMessage);
                    ModelState.AddModelError("", errorMessage ?? "Đăng nhập ngoài không thành công");
                    TempData["ErrorMessage"] = errorMessage ?? "Đăng nhập ngoài không thành công";
                    return RedirectToAction("Login");
                }

                if (!string.IsNullOrEmpty(picture) && string.IsNullOrEmpty(user.ProfileImageURL))
                {
                    try
                    {
                        user.ProfileImageURL = picture;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Updated profile picture from {Provider} for user {Email}", provider, email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating profile picture for user {Email}", email);
                    }
                }

                await SignInUserAsync(user, true);
                _logger.LogInformation("User {Email} successfully logged in with {Provider}", email, provider);
                return Redirect(returnUrl ?? Url.Action("Index", "Home"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during external login callback: {Message}", ex.Message);
                ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình đăng nhập");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình đăng nhập: " + ex.Message;
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string name, string username, string email, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, errorMessage = "Vui lòng điền đầy đủ thông tin" });
                    ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin");
                    return View();
                }

                if (await _userService.IsEmailTakenAsync(email))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, errorMessage = "Email đã được sử dụng" });
                    ModelState.AddModelError("Email", "Email đã được sử dụng");
                    return View();
                }

                if (await _userService.IsUsernameTakenAsync(username))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, errorMessage = "Tên người dùng đã được sử dụng" });
                    ModelState.AddModelError("Username", "Tên người dùng đã được sử dụng");
                    return View();
                }

                var existingUser = await _userService.GetUserByEmailAsync(email);
                if (existingUser != null && await _userService.IsExternalAccountAsync(email))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, errorMessage = "Email này đã được sử dụng bởi tài khoản Google hoặc Facebook" });
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản Google hoặc Facebook");
                    return View();
                }

                var success = await _userService.RegisterAsync(name, username, email, password);
                if (!success)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, errorMessage = "Đăng ký không thành công. Vui lòng thử lại sau." });
                    ModelState.AddModelError("", "Đăng ký không thành công. Vui lòng thử lại sau.");
                    return View();
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = "Đăng ký thành công. Vui lòng đăng nhập." });
                TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Register action for email: {Email}", email);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, errorMessage = "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau." });
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau.");
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserId");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Email không được để trống" });
                    ModelState.AddModelError("", "Email không được để trống");
                    return View();
                }

                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
                if (!emailRegex.IsMatch(email))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Email không hợp lệ" });
                    ModelState.AddModelError("", "Email không hợp lệ");
                    return View();
                }

                if (!await _userService.CanResetPasswordAsync(email))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Tài khoản này không thể đặt lại mật khẩu" });
                    ModelState.AddModelError("", "Tài khoản này không thể đặt lại mật khẩu");
                    return View();
                }

                var success = await _userService.GeneratePasswordResetTokenAsync(email);
                if (!success)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Không thể gửi email đặt lại mật khẩu. Vui lòng thử lại sau." });
                    ModelState.AddModelError("", "Không thể gửi email đặt lại mật khẩu. Vui lòng thử lại sau.");
                    return View();
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true, message = "Vui lòng kiểm tra email của bạn để đặt lại mật khẩu" });
                TempData["SuccessMessage"] = "Vui lòng kiểm tra email của bạn để đặt lại mật khẩu";
                return RedirectToAction("ResetPasswordConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong ForgotPassword action cho email: {Email}", email);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau." });
                ModelState.AddModelError("", "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau.");
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token không được cung cấp");
                return RedirectToAction("Login");
            }

            if (await _userService.IsPasswordResetTokenExpiredAsync(token))
            {
                _logger.LogWarning("Token đã hết hạn");
                TempData["ErrorMessage"] = "Link đặt lại mật khẩu đã hết hạn. Vui lòng yêu cầu link mới.";
                return RedirectToAction("ForgotPassword");
            }

            if (!await _userService.ValidatePasswordResetTokenAsync(token))
            {
                _logger.LogWarning("Token không hợp lệ");
                TempData["ErrorMessage"] = "Link đặt lại mật khẩu không hợp lệ. Vui lòng yêu cầu link mới.";
                return RedirectToAction("ForgotPassword");
            }

            ViewData["Token"] = token;
            return View();
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string newPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Token không được cung cấp");
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Token không hợp lệ" });
                    TempData["ErrorMessage"] = "Token không hợp lệ";
                    return RedirectToAction("ForgotPassword");
                }

                if (string.IsNullOrEmpty(newPassword))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Mật khẩu mới không được để trống" });
                    ModelState.AddModelError("", "Mật khẩu mới không được để trống");
                    return View();
                }

                if (newPassword.Length < 6)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Mật khẩu phải có ít nhất 6 ký tự" });
                    ModelState.AddModelError("", "Mật khẩu phải có ít nhất 6 ký tự");
                    return View();
                }

                if (!await _userService.ValidatePasswordResetTokenAsync(token))
                {
                    _logger.LogWarning("Token không hợp lệ hoặc không tồn tại: {Token}", token);
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Link đặt lại mật khẩu không hợp lệ" });
                    TempData["ErrorMessage"] = "Link đặt lại mật khẩu không hợp lệ. Vui lòng yêu cầu link mới.";
                    return RedirectToAction("ForgotPassword");
                }

                if (await _userService.IsPasswordResetTokenExpiredAsync(token))
                {
                    _logger.LogWarning("Token đã hết hạn: {Token}", token);
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Link đặt lại mật khẩu đã hết hạn" });
                    TempData["ErrorMessage"] = "Link đặt lại mật khẩu đã hết hạn. Vui lòng yêu cầu link mới.";
                    return RedirectToAction("ForgotPassword");
                }

                var success = await _userService.ResetPasswordAsync(token, newPassword);
                if (!success)
                {
                    _logger.LogError("Không thể đặt lại mật khẩu cho token: {Token}", token);
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Không thể đặt lại mật khẩu. Vui lòng thử lại sau." });
                    ModelState.AddModelError("", "Không thể đặt lại mật khẩu. Vui lòng thử lại sau.");
                    return View();
                }

                await _userService.InvalidatePasswordResetTokenAsync(token);
                _logger.LogInformation("Đặt lại mật khẩu thành công cho token: {Token}", token);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = true, message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại." });
                TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong ResetPassword action cho token: {Token}", token);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") return Json(new { success = false, errorMessage = "Đã xảy ra lỗi khi đặt lại mật khẩu. Vui lòng thử lại sau." });
                ModelState.AddModelError("", "Đã xảy ra lỗi khi đặt lại mật khẩu. Vui lòng thử lại sau.");
                return View();
            }
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Unauthorized("Invalid user identifier.");
            }

            _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            if (user == null) return NotFound();

            // Ensure user has a rank
            if (user.RankId == null)
            {
                user.RankId = 1;
                await _context.SaveChangesAsync();
            }

            ViewBag.UserProfileImage = user.ProfileImageURL;
            ViewBag.UserRank = user.Rank?.RankName ?? "Thành viên";
            ViewBag.NextRank = await _context.Ranks
                .Where(r => r.MinTotalSpent > user.TotalSpent)
                .OrderBy(r => r.MinTotalSpent)
                .FirstOrDefaultAsync();

            return View(user);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string name, string phone)
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
                if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng" });

                user.Name = name;
                user.Phone = phone;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật thông tin người dùng");
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật thông tin" });
            }
        }

        [HttpGet]
        [Authorize]
        [Route("Account/Orders")]
        public async Task<IActionResult> Orders()
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
                if (user == null) return NotFound();

                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.ProductImages)
                    .Where(o => o.UserID == user.UserID)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                ViewBag.UserProfileImage = user.ProfileImageURL;
                ViewBag.UserRank = user.Rank?.RankName ?? "Thành viên";
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders for user {Email}", User.Identity.Name);
                return View(new List<Order>());
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Unauthorized("Invalid user identifier.");
            }

            _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            if (user == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == user.UserID);

            if (order == null) return NotFound();

            ViewBag.UserProfileImage = user.ProfileImageURL;
            ViewBag.UserRank = user.Rank?.RankName ?? "Thành viên";
            return View(order);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Unauthorized("Invalid user identifier.");
            }

            _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng" });

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == user.UserID);

            if (order == null) return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            if (order.Status != "Pending" || (DateTime.Now - order.CreatedAt).TotalHours > 1)
                return Json(new { success = false, message = "Không thể hủy đơn hàng" });

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Hủy đơn hàng thành công" });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Addresses()
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Unauthorized("Invalid user identifier.");
            }

            _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            var addresses = await _context.UserAddresses.Where(a => a.UserID == user.UserID).ToListAsync();

            ViewBag.UserProfileImage = user.ProfileImageURL;
            ViewBag.UserRank = user.Rank?.RankName ?? "Thành viên";
            return View(addresses);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(UserAddress address)
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Unauthorized("Invalid user identifier.");
            }

            _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            address.UserID = user.UserID;
            address.CreatedAt = DateTime.Now;
            _context.UserAddresses.Add(address);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Unauthorized("Invalid user identifier.");
            }

            _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            var address = await _context.UserAddresses.FirstOrDefaultAsync(a => a.AddressID == id && a.UserID == user.UserID);
            if (address != null)
            {
                _context.UserAddresses.Remove(address);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Wishlist()
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Unauthorized("Invalid user identifier.");
            }

            _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            if (user == null) return NotFound();

            var wishlist = await _context.Wishlists
                .Include(w => w.WishlistItems)
                    .ThenInclude(wi => wi.Product)
                        .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(w => w.UserID == user.UserID);

            if (wishlist == null)
            {
                wishlist = new Wishlist { UserID = user.UserID };
                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();
            }

            ViewBag.UserProfileImage = user.ProfileImageURL;
            ViewBag.UserRank = user.Rank?.RankName ?? "Thành viên";
            return View(wishlist);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromWishlist(int id)
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
                if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng" });

                var wishlistItem = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.WishlistID == id && w.UserID == user.UserID);

                if (wishlistItem == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm trong danh sách yêu thích" });

                _context.Wishlists.Remove(wishlistItem);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa sản phẩm khỏi danh sách yêu thích");
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sản phẩm" });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Settings()
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Unauthorized("Invalid user identifier.");
            }

            _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            ViewBag.CanChangePassword = await _userService.HasPasswordAuthAsync(user.Email);
            ViewBag.UserProfileImage = user.ProfileImageURL;
            ViewBag.UserRank = user.Rank?.RankName ?? "Thành viên";
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null)
            {
                _logger.LogError("Email claim not found in user claims");
                return Unauthorized("Invalid user identifier.");
            }

            _logger.LogInformation("Email claim found: {Email}", emailClaim.Value);

            var user = await _userService.GetUserByEmailAsync(emailClaim.Value);
            if (!await _userService.HasPasswordAuthAsync(user.Email))
                return Json(new { success = false, message = "Tài khoản Google/Facebook không thể đổi mật khẩu" });

            var authMethod = await _context.UserAuthMethods
                .FirstOrDefaultAsync(uam => uam.UserID == user.UserID && uam.AuthType == "Password");

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, authMethod.AuthKey))
                return Json(new { success = false, message = "Mật khẩu hiện tại không đúng" });

            authMethod.AuthKey = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đổi mật khẩu thành công" });
        }

        private async Task SignInUserAsync(User user, bool isPersistent)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTime.UtcNow.AddDays(30) : null
            });

            HttpContext.Session.SetString("UserId", user.UserID.ToString());
            HttpContext.Session.SetString("UserRole", user.Role.ToString());
        }
    }
}