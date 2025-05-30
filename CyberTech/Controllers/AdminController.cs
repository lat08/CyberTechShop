using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CyberTech.Data;
using CyberTech.Models;

namespace CyberTechShop.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            // Check if user is already logged in
            if (HttpContext.Session.GetString("StaffID") != null)
            {
                return RedirectToAction("Index", "ProductManage");
            }

            var email = Request.Cookies["RememberMe_Email"];

            if (!string.IsNullOrEmpty(email))
            {
                if (email == "admin@gmail.com")
                {
                    HttpContext.Session.SetString("StaffID", "1");
                    HttpContext.Session.SetString("StaffFullName", "Admin");
                    HttpContext.Session.SetString("Email", "admin@gmail.com");
                    HttpContext.Session.SetString("Role", "Admin");

                    return RedirectToAction("Index", "ProductManage");
                }
                else
                {
                    Response.Cookies.Delete("RememberMe_Email");
                }
            }

            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password, bool rememberMe)
        {
            // Tạm ẩn việc xác thực reCAPTCHA
            // if (!await _recaptchaService.VerifyAsync(recaptchaToken))
            // {
            //     ModelState.AddModelError("", "Xác thực reCAPTCHA thất bại");
            //     return View();
            // }

            if (email == "admin@gmail.com" && password == "123123")
            {
                HttpContext.Session.SetString("StaffID", "1");
                HttpContext.Session.SetString("StaffFullName", "Admin");
                HttpContext.Session.SetString("Email", "admin@gmail.com");
                HttpContext.Session.SetString("Role", "Admin");

                if (rememberMe)
                {
                    var cookieOptions = new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(30),
                        HttpOnly = true
                    };
                    Response.Cookies.Append("RememberMe_Email", email, cookieOptions);
                }

                return Json(new { redirectUrl = Url.Action("Index", "ProductManage") });
            }

            return Json(new { error = "Email hoặc mật khẩu không đúng" });
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("RememberMe_Email");
            return RedirectToAction("Login");
        }
    }
}
