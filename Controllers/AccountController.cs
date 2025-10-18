using FamilyTreePro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FamilyTreePro.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ViewBag.Error = "اسم المستخدم وكلمة المرور مطلوبان";
                    return View();
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

                if (user == null)
                {
                    ViewBag.Error = "اسم المستخدم أو كلمة المرور غير صحيح";
                    return View();
                }

                // تسجيل الدخول العادي
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);

                // نستخدم اسم المستخدم لتحديد المدير (مؤقتاً)
                bool isAdmin = user.Username.ToLower() == "admin";
                HttpContext.Session.SetString("IsAdmin", isAdmin.ToString());

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تسجيل الدخول");
                ViewBag.Error = "حدث خطأ أثناء تسجيل الدخول";
                return View();
            }
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                    {
                        ViewBag.Error = "اسم المستخدم موجود مسبقاً";
                        return View(user);
                    }

                    user.CreatedDate = DateTime.Now;
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // تسجيل الدخول بعد التسجيل
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("IsAdmin", "False");

                    return RedirectToAction("Index", "Home");
                }
                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التسجيل");
                ViewBag.Error = "حدث خطأ أثناء التسجيل";
                return View(user);
            }
        }

        // التحقق من صلاحية المدير
        private async Task<bool> IsAdminAsync()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("المستخدم غير مسجل دخول");
                    return false;
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"المستخدم برقم {userId} غير موجود في قاعدة البيانات");
                    return false;
                }

                bool isAdmin = user.IsAdmin;
                _logger.LogInformation($"التحقق من صلاحية المستخدم {user.Username}: IsAdmin = {isAdmin}");

                return isAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من صلاحية المدير");
                return false;
            }
        }

        // عرض تفاصيل المستخدم
        public async Task<IActionResult> UserDetails(int id)
        {
            try
            {
                if (!await IsAdminAsync())
                {
                    TempData["ErrorMessage"] = "ليس لديك صلاحية للوصول إلى هذه الصفحة";
                    return RedirectToAction("Index", "Home");
                }

                var user = await _context.Users
                    .Include(u => u.FamilyTrees)
                    .ThenInclude(ft => ft.Persons)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "المستخدم غير موجود";
                    return RedirectToAction("Users");
                }

                // استخدام ViewData بدلاً من النموذج المباشر
                ViewData["User"] = user;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في تحميل تفاصيل المستخدم {id}");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل البيانات: " + ex.Message;
                return RedirectToAction("Users");
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}