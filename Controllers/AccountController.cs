using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FamilyTreePro.Models;

namespace FamilyTreePro.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // تسجيل الدخول
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "اسم المستخدم أو كلمة المرور غير صحيحة";
            return View();
        }

        // تسجيل جديد
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                // التحقق من عدم وجود اسم مستخدم مكرر
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                {
                    ModelState.AddModelError("Username", "اسم المستخدم موجود مسبقاً");
                    return View(user);
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // تسجيل الدخول تلقائياً بعد التسجيل
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);

                return RedirectToAction("Index", "Home");
            }
            return View(user);
        }

        // تسجيل الخروج
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}