using FamilyTreePro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FamilyTreePro.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
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

        // صفحة إدارة المستخدمين الرئيسية
        // صفحة إدارة المستخدمين الرئيسية
        public async Task<IActionResult> Users()
        {
            try
            {
                _logger.LogInformation("محاولة الدخول إلى صفحة إدارة المستخدمين");

                if (!await IsAdminAsync())
                {
                    TempData["ErrorMessage"] = "ليس لديك صلاحية مدير النظام للوصول إلى هذه الصفحة";
                    return RedirectToAction("Index", "Home");
                }

                var users = await _context.Users
                    .OrderByDescending(u => u.CreatedDate)
                    .ToListAsync();

                _logger.LogInformation($"تم تحميل صفحة إدارة المستخدمين بنجاح. عدد المستخدمين: {users.Count}");

                // استخدام ViewData بدلاً من تمرير النموذج مباشرة
                ViewData["Users"] = users;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل صفحة إدارة المستخدمين");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل البيانات: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // تفعيل/تعطيل المستخدم
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int id, bool activate)
        {
            try
            {
                if (!await IsAdminAsync())
                {
                    return Json(new { success = false, message = "غير مصرح - تحتاج صلاحية مدير" });
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "المستخدم غير موجود" });
                }

                // منع تعطيل المديرين
                if (user.IsAdmin && !activate)
                {
                    return Json(new { success = false, message = "لا يمكن تعطيل حساب مدير النظام" });
                }

                user.IsActive = activate;
                await _context.SaveChangesAsync();

                var action = activate ? "تفعيل" : "تعطيل";
                _logger.LogInformation($"تم {action} المستخدم: {user.Username}");

                return Json(new { success = true, message = $"تم {action} المستخدم بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في تغيير حالة المستخدم {id}");
                return Json(new { success = false, message = "حدث خطأ أثناء تغيير الحالة: " + ex.Message });
            }
        }

        // ترقية/إلغاء ترقية المدير
        [HttpPost]
        public async Task<IActionResult> ToggleAdminRole(int id, bool makeAdmin)
        {
            try
            {
                if (!await IsAdminAsync())
                {
                    return Json(new { success = false, message = "غير مصرح - تحتاج صلاحية مدير" });
                }

                var currentUserId = HttpContext.Session.GetInt32("UserId");
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return Json(new { success = false, message = "المستخدم غير موجود" });
                }

                // منع إلغاء صلاحية المدير لنفسه
                if (user.Id == currentUserId && !makeAdmin)
                {
                    return Json(new { success = false, message = "لا يمكن إلغاء صلاحيات المدير لحسابك الشخصي" });
                }

                user.IsAdmin = makeAdmin;
                await _context.SaveChangesAsync();

                var action = makeAdmin ? "ترقيته إلى مدير" : "إلغاء صلاحية المدير";
                _logger.LogInformation($"تم {action} للمستخدم: {user.Username}");

                return Json(new { success = true, message = $"تم {action} بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في تغيير صلاحية المستخدم {id}");
                return Json(new { success = false, message = "حدث خطأ أثناء تغيير الصلاحية: " + ex.Message });
            }
        }

        // حذف المستخدم
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                if (!await IsAdminAsync())
                {
                    return Json(new { success = false, message = "غير مصرح - تحتاج صلاحية مدير" });
                }

                var currentUserId = HttpContext.Session.GetInt32("UserId");
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return Json(new { success = false, message = "المستخدم غير موجود" });
                }

                // منع حذف المديرين
                if (user.IsAdmin)
                {
                    return Json(new { success = false, message = "لا يمكن حذف حساب مدير النظام" });
                }

                // منع المستخدم من حذف نفسه
                if (user.Id == currentUserId)
                {
                    return Json(new { success = false, message = "لا يمكن حذف حسابك الشخصي" });
                }

                // حذف الشجرات التابعة للمستخدم أولاً
                var userTrees = _context.FamilyTrees.Where(ft => ft.UserId == id);
                _context.FamilyTrees.RemoveRange(userTrees);

                // ثم حذف المستخدم
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"تم حذف المستخدم: {user.Username} وجميع بياناته");

                return Json(new { success = true, message = "تم حذف المستخدم وجميع بياناته بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في حذف المستخدم {id}");
                return Json(new { success = false, message = "حدث خطأ أثناء حذف المستخدم: " + ex.Message });
            }
        }

        // إضافة مستخدم جديد
        [HttpPost]
        public async Task<IActionResult> AddUser(User user)
        {
            try
            {
                if (!await IsAdminAsync())
                {
                    TempData["ErrorMessage"] = "غير مصرح - تحتاج صلاحية مدير";
                    return RedirectToAction("Users");
                }

                if (ModelState.IsValid)
                {
                    // التحقق من عدم وجود اسم مستخدم مكرر
                    if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                    {
                        TempData["ErrorMessage"] = "اسم المستخدم موجود مسبقاً";
                        return RedirectToAction("Users");
                    }

                    user.CreatedDate = DateTime.Now;
                    user.IsActive = true;
                    user.IsAdmin = false; // المستخدمون الجدد ليسوا مدراء افتراضياً
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"تم إضافة مستخدم جديد بواسطة المدير: {user.Username}");
                    TempData["SuccessMessage"] = "تم إضافة المستخدم الجديد بنجاح";
                }
                else
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    TempData["ErrorMessage"] = $"بيانات المستخدم غير صالحة: {errors}";
                }

                return RedirectToAction("Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إضافة مستخدم جديد");
                TempData["ErrorMessage"] = "حدث خطأ أثناء إضافة المستخدم: " + ex.Message;
                return RedirectToAction("Users");
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

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في تحميل تفاصيل المستخدم {id}");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل البيانات: " + ex.Message;
                return RedirectToAction("Users");
            }
        }
        // الملف الشخصي للمستخدم الحالي
        // الملف الشخصي للمستخدم الحالي
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    TempData["ErrorMessage"] = "يجب تسجيل الدخول أولاً";
                    return RedirectToAction("Login", "Account");
                }

                var user = await _context.Users
                    .Include(u => u.FamilyTrees)
                    .ThenInclude(ft => ft.Persons)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "المستخدم غير موجود";
                    return RedirectToAction("Index", "Home");
                }

                ViewData["User"] = user;
                return View(); // سيبحث عن Views/Admin/Profile.cshtml
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل الملف الشخصي");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل البيانات: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }
        // إحصائيات النظام
        // إحصائيات النظام
        // إحصائيات النظام
        // إحصائيات النظام
        public async Task<IActionResult> Statistics()
        {
            try
            {
                if (!await IsAdminAsync())
                {
                    TempData["ErrorMessage"] = "ليس لديك صلاحية للوصول إلى هذه الصفحة";
                    return RedirectToAction("Index", "Home");
                }

                // الحصول على إحصائيات المستخدمين مع الشجرات والأفراد
                var usersWithStats = await _context.Users
                    .Include(u => u.FamilyTrees)
                    .ThenInclude(ft => ft.Persons)
                    .Select(u => new
                    {
                        User = u,
                        TreeCount = u.FamilyTrees.Count,
                        PersonCount = u.FamilyTrees.Sum(ft => ft.Persons.Count)
                    })
                    .ToListAsync();

                ViewBag.TotalUsers = await _context.Users.CountAsync();
                ViewBag.ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
                ViewBag.AdminUsers = await _context.Users.CountAsync(u => u.IsAdmin);
                ViewBag.TotalTrees = await _context.FamilyTrees.CountAsync();
                ViewBag.TotalPersons = await _context.Persons.CountAsync();
                ViewBag.RecentRegistrations = await _context.Users
                    .Where(u => u.CreatedDate >= DateTime.Now.AddDays(-7))
                    .CountAsync();

                ViewBag.UsersWithStats = usersWithStats;
                ViewBag.LatestUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedDate)
                    .Take(5)
                    .ToListAsync();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل إحصائيات النظام");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل الإحصائيات: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        // تحديث بيانات المستخدم
        [HttpPost]
        public async Task<IActionResult> UpdateUser(User updatedUser)
        {
            try
            {
                if (!await IsAdminAsync())
                {
                    return Json(new { success = false, message = "غير مصرح - تحتاج صلاحية مدير" });
                }

                var user = await _context.Users.FindAsync(updatedUser.Id);
                if (user == null)
                {
                    return Json(new { success = false, message = "المستخدم غير موجود" });
                }

                // تحديث البيانات المسموح بها
                user.FullName = updatedUser.FullName;
                user.Email = updatedUser.Email;
                user.IsActive = updatedUser.IsActive;

                // تحديث كلمة المرور فقط إذا تم تقديمها
                if (!string.IsNullOrEmpty(updatedUser.Password))
                {
                    user.Password = updatedUser.Password;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"تم تحديث بيانات المستخدم: {user.Username}");
                return Json(new { success = true, message = "تم تحديث بيانات المستخدم بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في تحديث بيانات المستخدم {updatedUser.Id}");
                return Json(new { success = false, message = "حدث خطأ أثناء تحديث البيانات: " + ex.Message });
            }
        }

        // البحث عن المستخدمين
        [HttpGet]
        public async Task<IActionResult> SearchUsers(string searchTerm)
        {
            try
            {
                if (!await IsAdminAsync())
                {
                    return Json(new { success = false, message = "غير مصرح" });
                }

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    var allUsers = await _context.Users
                        .OrderByDescending(u => u.CreatedDate)
                        .Take(50)
                        .ToListAsync();

                    return Json(new { success = true, users = allUsers });
                }

                var users = await _context.Users
                    .Where(u => u.Username.Contains(searchTerm) ||
                               u.FullName.Contains(searchTerm) ||
                               u.Email.Contains(searchTerm))
                    .OrderByDescending(u => u.CreatedDate)
                    .Take(10)
                    .ToListAsync();

                return Json(new { success = true, users = users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في البحث عن المستخدمين: {searchTerm}");
                return Json(new { success = false, message = "حدث خطأ أثناء البحث: " + ex.Message });
            }
        }

        // إنشاء مستخدم مدير افتراضي (للاستخدام في حالات الطوارئ)
        [HttpPost]
        public async Task<IActionResult> CreateDefaultAdmin()
        {
            try
            {
                // التحقق إذا كان هناك مدير موجود بالفعل
                var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.IsAdmin);
                if (existingAdmin != null)
                {
                    return Json(new { success = false, message = "يوجد مدير نظام بالفعل" });
                }

                var adminUser = new User
                {
                    Username = "admin",
                    Password = "admin123",
                    FullName = "مدير النظام",
                    Email = "admin@familytree.com",
                    IsAdmin = true,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("تم إنشاء مستخدم المدير الافتراضي");
                return Json(new { success = true, message = "تم إنشاء مستخدم المدير الافتراضي بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء مستخدم المدير الافتراضي");
                return Json(new { success = false, message = "حدث خطأ أثناء إنشاء المدير: " + ex.Message });
            }
        }

        // إصلاح جميع المستخدمين (للاستخدام بعد تحديث قاعدة البيانات)
        [HttpPost]
        public async Task<IActionResult> FixAllUsers()
        {
            try
            {
                if (!await IsAdminAsync())
                {
                    return Json(new { success = false, message = "غير مصرح - تحتاج صلاحية مدير" });
                }

                var users = await _context.Users.ToListAsync();
                int fixedCount = 0;

                foreach (var user in users)
                {
                    bool needsUpdate = false;

                    // تعيين IsActive = true لجميع المستخدمين الحاليين
                    if (!user.IsActive)
                    {
                        user.IsActive = true;
                        needsUpdate = true;
                    }

                    // جعل المستخدم admin فقط هو المدير
                    if (user.Username.ToLower() == "admin" && !user.IsAdmin)
                    {
                        user.IsAdmin = true;
                        needsUpdate = true;
                    }
                    else if (user.Username.ToLower() != "admin" && user.IsAdmin)
                    {
                        user.IsAdmin = false;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        fixedCount++;
                    }
                }

                if (fixedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"تم إصلاح {fixedCount} مستخدم");
                }

                return Json(new
                {
                    success = true,
                    message = $"تم إصلاح {fixedCount} مستخدم بنجاح",
                    details = "تم تفعيل جميع المستخدمين وجعل admin فقط هو المدير"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إصلاح المستخدمين");
                return Json(new { success = false, message = "حدث خطأ أثناء إصلاح المستخدمين: " + ex.Message });
            }
        }
    }

    // نموذج عرض الإحصائيات
    public class AdminStatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int TotalTrees { get; set; }
        public int TotalPersons { get; set; }
        public int RecentRegistrations { get; set; }
        public List<User> LatestUsers { get; set; }
    }
}