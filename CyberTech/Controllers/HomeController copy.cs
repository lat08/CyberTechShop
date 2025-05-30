// using CyberTech.Models;
// using Microsoft.AspNetCore.Mvc;
// using System.Diagnostics;
// using Microsoft.Extensions.Logging;
// using CyberTech.Services;
// using System.Security.Claims;

// namespace CyberTech.Controllers
// {
//     public class Home2Controller : Controller
//     {
//         private readonly ILogger<HomeController> _logger;
//         private readonly IUserService _userService;

//         public Home2Controller(ILogger<HomeController> logger, IUserService userService)
//         {
//             _logger = logger;
//             _userService = userService;
//         }

//         public async Task<IActionResult> Index()
//         {
//             _logger.LogInformation("Home page visited");

//             if (User.Identity.IsAuthenticated)
//             {
//                 var email = User.FindFirst(ClaimTypes.Email)?.Value;
//                 if (!string.IsNullOrEmpty(email))
//                 {
//                     var user = await _userService.GetUserByEmailAsync(email);
//                     if (user != null)
//                     {
//                         ViewData["UserName"] = user.Name;
//                         ViewData["UserEmail"] = user.Email;
//                         ViewData["UserRole"] = user.Role;
//                         ViewData["ProfileImage"] = user.ProfileImageURL;
//                         ViewData["LastLogin"] = user.LastLoginAt?.ToString("dd/MM/yyyy HH:mm:ss");
//                     }
//                 }
//             }

//             return View();
//         }

//         public IActionResult Privacy()
//         {
//             return View();
//         }

//         [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//         public IActionResult Error()
//         {
//             return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
//         }
//     }
// }
