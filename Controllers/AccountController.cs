using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FamilyTreePro.Models;
using Microsoft.Extensions.Logging;

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

        // تسجيل الدخول - GET
        public IActionResult Login()
        {
            // إذا كان المستخدم مسجل دخول بالفعل، توجيه للصفحة الرئيسية
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // تسجيل الدخول - POST
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

                if (user != null)
                {
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("Username", user.Username);

                    // التحقق وإضافة شجرة افتراضية إذا لم توجد
                    await EnsureDefaultFamilyTree(user.Id);

                    _logger.LogInformation($"تم تسجيل دخول المستخدم: {username}");
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.Error = "اسم المستخدم أو كلمة المرور غير صحيحة";
                _logger.LogWarning($"محاولة تسجيل دخول فاشلة للمستخدم: {username}");
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تسجيل الدخول");
                ViewBag.Error = "حدث خطأ أثناء تسجيل الدخول";
                return View();
            }
        }

        // تسجيل جديد - GET
        public IActionResult Register()
        {
            // إذا كان المستخدم مسجل دخول بالفعل، توجيه للصفحة الرئيسية
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // تسجيل جديد - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // التحقق من عدم وجود اسم مستخدم مكرر
                    if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                    {
                        ModelState.AddModelError("Username", "اسم المستخدم موجود مسبقاً");
                        return View(user);
                    }

                    user.CreatedDate = DateTime.Now;
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // تسجيل الدخول تلقائياً بعد التسجيل
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("Username", user.Username);

                    // إنشاء شجرة افتراضية للمستخدم الجديد
                    await EnsureDefaultFamilyTree(user.Id);

                    _logger.LogInformation($"تم تسجيل مستخدم جديد: {user.Username}");
                    return RedirectToAction("Index", "Home");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تسجيل المستخدم الجديد");
                ViewBag.Error = "حدث خطأ أثناء التسجيل";
                return View(user);
            }
        }

        // تسجيل الخروج
        public IActionResult Logout()
        {
            var username = HttpContext.Session.GetString("Username");
            HttpContext.Session.Clear();
            _logger.LogInformation($"تم تسجيل خروج المستخدم: {username}");
            return RedirectToAction("Login");
        }

        // دالة مساعدة لإنشاء شجرة افتراضية
        // دالة مساعدة لإنشاء شجرة افتراضية
        private async Task EnsureDefaultFamilyTree(int userId)
        {
            try
            {
                // التحقق إذا كان للمستخدم شجرة بالفعل
                var hasTree = await _context.FamilyTrees
                    .AnyAsync(ft => ft.UserId == userId);

                _logger.LogInformation($"التحقق من الشجرات للمستخدم {userId}: يوجد {hasTree} شجرة");

                if (!hasTree)
                {
                    var defaultTree = new FamilyTree
                    {
                        Name = "شجرتي العائلية",
                        Description = "الشجرة العائلية الرئيسية",
                        Color = "#007bff",
                        UserId = userId,
                        CreatedDate = DateTime.Now
                    };

                    _context.FamilyTrees.Add(defaultTree);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"تم إنشاء شجرة افتراضية للمستخدم: {userId} برقم: {defaultTree.Id}");
                }
                else
                {
                    _logger.LogInformation($"المستخدم {userId} لديه شجرة بالفعل، لم يتم إنشاء جديدة");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء الشجرة الافتراضية");
            }
        }
    }
}