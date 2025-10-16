using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FamilyTreePro.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FamilyTreePro.Controllers  // تأكد من وجود namespace
{
    public class PersonController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PersonController> _logger;

        public PersonController(AppDbContext context, ILogger<PersonController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        // GET: إضافة فرد جديد
        [HttpGet]
        public IActionResult Create(int familyTreeId)
        {
            try
            {
                // التحقق من وجود المستخدم
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // التحقق من أن الشجرة تتبع للمستخدم
                var tree = _context.FamilyTrees.FirstOrDefault(ft => ft.Id == familyTreeId && ft.UserId == userId);
                if (tree == null)
                {
                    TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                    return RedirectToAction("Index", "Home");
                }

                var viewModel = new CreatePersonViewModel
                {
                    FamilyTreeId = familyTreeId
                };

                // تعبئة القوائم المنسدلة (بدون الأمهات)
                ViewBag.Occupations = _context.Occupations.ToList();
                ViewBag.Countries = _context.Countries.ToList();
                ViewBag.PotentialFathers = _context.Persons
                    .Where(p => p.FamilyTreeId == familyTreeId && p.Gender == "Male")
                    .ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل صفحة إضافة فرد جديد");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل الصفحة";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: إضافة فرد جديد
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePersonViewModel viewModel)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // تسجيل تفاصيل النموذج المستلم
            _logger.LogInformation($"🔍 بيانات النموذج المستلمة:");
            _logger.LogInformation($"   - الاسم: {viewModel.FirstName}");
            _logger.LogInformation($"   - اسم الأب: {viewModel.FatherName}");
            _logger.LogInformation($"   - اسم الجد: {viewModel.GrandFatherName}");
            _logger.LogInformation($"   - العائلة: {viewModel.LastName}");
            _logger.LogInformation($"   - الجنس: {viewModel.Gender}");
            _logger.LogInformation($"   - الشجرة: {viewModel.FamilyTreeId}");

            // التحقق من صحة النموذج
            _logger.LogInformation($"🔍 حالة ModelState: {ModelState.IsValid}");
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning($"❌ أخطاء التحقق: {string.Join(", ", errors)}");
                TempData["ErrorMessage"] = "البيانات غير صالحة. يرجى تصحيح الأخطاء أدناه.";

                await RepopulateViewBags(viewModel.FamilyTreeId);
                return View(viewModel);
            }

            // التحقق من وجود الشجرة
            var tree = await _context.FamilyTrees
                .FirstOrDefaultAsync(ft => ft.Id == viewModel.FamilyTreeId && ft.UserId == userId);

            if (tree == null)
            {
                _logger.LogWarning($"❌ الشجرة غير موجودة: {viewModel.FamilyTreeId} للمستخدم: {userId}");
                TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // إنشاء الشخص الجديد
                var person = new Person
                {
                    FirstName = viewModel.FirstName?.Trim(),
                    FatherName = viewModel.FatherName?.Trim(),
                    GrandFatherName = viewModel.GrandFatherName?.Trim(),
                    LastName = viewModel.LastName?.Trim(),
                    Nickname = viewModel.Nickname?.Trim(),
                    Gender = viewModel.Gender,
                    BirthDate = viewModel.BirthDate,
                    OccupationId = viewModel.OccupationId,
                    CountryId = viewModel.CountryId,
                    City = viewModel.City?.Trim(),
                    Notes = viewModel.Notes?.Trim(),
                    FamilyTreeId = viewModel.FamilyTreeId,
                    FatherId = viewModel.FatherId,
                    CreatedDate = DateTime.Now
                };

                _logger.LogInformation($"💾 محاولة حفظ الشخص: {person.FullName}");

                // حفظ البيانات
                _context.Persons.Add(person);
                int recordsAffected = await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ تم حفظ الشخص بنجاح! السجلات المتأثرة: {recordsAffected}, الرقم: {person.Id}");

                TempData["SuccessMessage"] = $"تم إضافة الفرد {person.FullName} بنجاح!";
                return RedirectToAction("Index", new { familyTreeId = viewModel.FamilyTreeId });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "❌ خطأ في قاعدة البيانات أثناء إضافة الفرد");
                _logger.LogError($"تفاصيل الخطأ الداخلية: {dbEx.InnerException?.Message}");

                TempData["ErrorMessage"] = "حدث خطأ في قاعدة البيانات. قد تكون البيانات مكررة أو غير صالحة.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ غير متوقع أثناء إضافة الفرد");
                TempData["ErrorMessage"] = $"حدث خطأ غير متوقع: {ex.Message}";
            }

            // في حالة الخطأ، إعادة تعبئة القوائم المنسدلة
            await RepopulateViewBags(viewModel.FamilyTreeId);
            return View(viewModel);
        }

        // أكشن لعرض قائمة الأشخاص في شجرة معينة
        public async Task<IActionResult> Index(int familyTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // التحقق من أن الشجرة تتبع للمستخدم
            var tree = await _context.FamilyTrees
                .FirstOrDefaultAsync(ft => ft.Id == familyTreeId && ft.UserId == userId);
            if (tree == null)
            {
                TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                return RedirectToAction("Index", "Home");
            }

            // تسجيل معلومات التصحيح
            _logger.LogInformation($"جلب الأفراد للشجرة: {familyTreeId} للمستخدم: {userId}");

            var persons = await _context.Persons
                .Include(p => p.Occupation)
                .Include(p => p.Country)
                .Include(p => p.Father)
                .Where(p => p.FamilyTreeId == familyTreeId)
                .OrderBy(p => p.FirstName)
                .ThenBy(p => p.FatherName)
                .ToListAsync();

            _logger.LogInformation($"تم العثور على {persons.Count} فرد في الشجرة");

            ViewBag.FamilyTreeId = familyTreeId;
            ViewBag.FamilyTreeName = tree.Name;

            return View(persons);
        }

        // دالة مساعدة لإعادة تعبئة ViewBags
        private async Task RepopulateViewBags(int familyTreeId)
        {
            ViewBag.Occupations = await _context.Occupations.ToListAsync();
            ViewBag.Countries = await _context.Countries.ToListAsync();
            ViewBag.PotentialFathers = await _context.Persons
                .Where(p => p.FamilyTreeId == familyTreeId && p.Gender == "Male")
                .ToListAsync();
        }

        // أكشن إضافي: تفاصيل الشخص
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var person = await _context.Persons
                    .Include(p => p.Occupation)
                    .Include(p => p.Country)
                    .Include(p => p.Father)
                    // .Include(p => p.Mother) // تم إزالته مؤقتاً
                    .Include(p => p.FamilyTree)
                    .FirstOrDefaultAsync(p => p.Id == id && p.FamilyTree.UserId == userId);

                if (person == null)
                {
                    TempData["ErrorMessage"] = "الفرد غير موجود أو لا تملك صلاحية الوصول له";
                    return RedirectToAction("Index", "Home");
                }

                return View(person);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل تفاصيل الفرد");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل التفاصيل";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}