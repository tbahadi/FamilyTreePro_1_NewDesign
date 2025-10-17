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
        // GET: حذف فرد
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var person = await _context.Persons
                    .Include(p => p.FamilyTree)
                    .Include(p => p.Children)
                    .FirstOrDefaultAsync(p => p.Id == id && p.FamilyTree.UserId == userId);

                if (person == null)
                {
                    TempData["ErrorMessage"] = "الفرد غير موجود أو لا تملك صلاحية الوصول له";
                    return RedirectToAction("Index", "Home");
                }

                // التحقق من وجود أبناء
                if (person.Children.Any())
                {
                    ViewBag.CanDelete = false;
                    ViewBag.ChildrenCount = person.Children.Count;
                }
                else
                {
                    ViewBag.CanDelete = true;
                }

                return View(person);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل صفحة الحذف");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل صفحة الحذف";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: حذف فرد
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var person = await _context.Persons
                    .Include(p => p.FamilyTree)
                    .Include(p => p.Children)
                    .FirstOrDefaultAsync(p => p.Id == id && p.FamilyTree.UserId == userId);

                if (person == null)
                {
                    TempData["ErrorMessage"] = "الفرد غير موجود";
                    return RedirectToAction("Index", "Home");
                }

                var familyTreeId = person.FamilyTreeId;
                var personName = person.FullName;

                // التحقق من وجود أبناء
                if (person.Children.Any())
                {
                    TempData["ErrorMessage"] = "لا يمكن حذف الفرد لأنه لديه أبناء. يرجى حذف الأبناء أولاً.";
                    return RedirectToAction("Delete", new { id = id });
                }

                _context.Persons.Remove(person);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"تم حذف الفرد {personName} بنجاح!";
                return RedirectToAction("Index", new { familyTreeId = familyTreeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء حذف الفرد");
                TempData["ErrorMessage"] = $"حدث خطأ أثناء الحذف: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }
        // GET: إضافة فرد جديد
        [HttpGet]
        public IActionResult Create(int familyTreeId, int? fatherId = null)
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
                FamilyTreeId = familyTreeId,
                FatherId = fatherId // تعيين الأب إذا كان موجوداً
            };

            // تعبئة القوائم المنسدلة
            ViewBag.Occupations = _context.Occupations.ToList();
            ViewBag.Countries = _context.Countries.ToList();
            ViewBag.PotentialFathers = _context.Persons
                .Where(p => p.FamilyTreeId == familyTreeId && p.Gender == "Male")
                .ToList();

            // إذا كان هناك أب محدد، عرض معلوماته
            if (fatherId.HasValue)
            {
                var father = _context.Persons.Find(fatherId.Value);
                ViewBag.FatherName = father?.FullName;
                ViewBag.IsAddingChild = true;
            }

            return View(viewModel);
        }

        // GET: تعديل فرد
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var person = await _context.Persons
                    .Include(p => p.FamilyTree)
                    .FirstOrDefaultAsync(p => p.Id == id && p.FamilyTree.UserId == userId);

                if (person == null)
                {
                    TempData["ErrorMessage"] = "الفرد غير موجود أو لا تملك صلاحية الوصول له";
                    return RedirectToAction("Index", "Home");
                }

                // تحويل Person إلى EditPersonViewModel
                var viewModel = new EditPersonViewModel
                {
                    Id = person.Id,
                    FirstName = person.FirstName,
                    FatherName = person.FatherName,
                    GrandFatherName = person.GrandFatherName,
                    LastName = person.LastName,
                    Nickname = person.Nickname,
                    Gender = person.Gender,
                    BirthDate = person.BirthDate,
                    OccupationId = person.OccupationId,
                    CountryId = person.CountryId,
                    City = person.City,
                    Notes = person.Notes,
                    FamilyTreeId = person.FamilyTreeId,
                    FatherId = person.FatherId
                };

                // تعبئة القوائم المنسدلة
                await RepopulateViewBags(person.FamilyTreeId);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل صفحة التعديل");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل صفحة التعديل";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: تعديل فرد
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditPersonViewModel viewModel)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation($"🔍 بدء تعديل الفرد {viewModel.Id}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ نموذج التعديل غير صالح");
                await RepopulateViewBags(viewModel.FamilyTreeId);
                return View(viewModel);
            }

            try
            {
                var person = await _context.Persons
                    .Include(p => p.FamilyTree)
                    .FirstOrDefaultAsync(p => p.Id == viewModel.Id && p.FamilyTree.UserId == userId);

                if (person == null)
                {
                    TempData["ErrorMessage"] = "الفرد غير موجود أو لا تملك صلاحية الوصول له";
                    return RedirectToAction("Index", "Home");
                }

                // تحديث البيانات
                person.FirstName = viewModel.FirstName?.Trim();
                person.FatherName = viewModel.FatherName?.Trim();
                person.GrandFatherName = viewModel.GrandFatherName?.Trim();
                person.LastName = viewModel.LastName?.Trim();
                person.Nickname = viewModel.Nickname?.Trim();
                person.Gender = viewModel.Gender;
                person.BirthDate = viewModel.BirthDate;
                person.OccupationId = viewModel.OccupationId;
                person.CountryId = viewModel.CountryId;
                person.City = viewModel.City?.Trim();
                person.Notes = viewModel.Notes?.Trim();
                person.FatherId = viewModel.FatherId;

                _context.Persons.Update(person);
                int recordsAffected = await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ تم تعديل الفرد بنجاح! السجلات المتأثرة: {recordsAffected}");

                TempData["SuccessMessage"] = $"تم تعديل الفرد {person.FullName} بنجاح!";
                return RedirectToAction("Index", new { familyTreeId = viewModel.FamilyTreeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء تعديل الفرد");
                TempData["ErrorMessage"] = $"حدث خطأ أثناء التعديل: {ex.Message}";

                await RepopulateViewBags(viewModel.FamilyTreeId);
                return View(viewModel);
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
                // إنشاء الشخص الجديد مع تعيين جميع القيم
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
                    Photo = string.Empty, // قيمة افتراضية واضحة
                    CreatedDate = DateTime.Now
                };

                _logger.LogInformation($"💾 محاولة حفظ الشخص: {person.FullName}");
                _logger.LogInformation($"   - Photo: '{person.Photo}'");
                _logger.LogInformation($"   - City: '{person.City}'");
                _logger.LogInformation($"   - Notes: '{person.Notes}'");

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
        // عرض الشجرة الهرمية
        public async Task<IActionResult> FamilyTreeView(int familyTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // التحقق من أن الشجرة تتبع للمستخدم
                var tree = await _context.FamilyTrees
                    .FirstOrDefaultAsync(ft => ft.Id == familyTreeId && ft.UserId == userId);

                if (tree == null)
                {
                    TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                    return RedirectToAction("Index", "Home");
                }

                // جلب جميع أفراد الشجرة مع العلاقات
                var persons = await _context.Persons
                    .Include(p => p.Father)
                    .Include(p => p.Children)
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .ToListAsync();

                // العثور على الجذر (الأشخاص الذين ليس لهم أب)
                var rootPersons = persons.Where(p => p.FatherId == null).ToList();

                ViewBag.FamilyTreeId = familyTreeId;
                ViewBag.FamilyTreeName = tree.Name;
                ViewBag.RootPersons = rootPersons;
                ViewBag.AllPersons = persons;

                _logger.LogInformation($"تم تحميل {persons.Count} فرد للشجرة الهرمية، منهم {rootPersons.Count} جذر");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل الشجرة الهرمية");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل الشجرة الهرمية";
                return RedirectToAction("Index", "Home");
            }
        }
        // أكشن لعرض قائمة الأشخاص في شجرة معينة
        // أكشن لعرض قائمة الأشخاص في شجرة معينة
        // أكشن لعرض قائمة الأشخاص في شجرة معينة
        public async Task<IActionResult> Index(int familyTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation($"🔍 بدء تحميل الأفراد للشجرة {familyTreeId} للمستخدم {userId}");

            try
            {
                // التحقق أولاً من وجود الشجرة وتتبعها للمستخدم
                var tree = await _context.FamilyTrees
                    .FirstOrDefaultAsync(ft => ft.Id == familyTreeId && ft.UserId == userId);

                if (tree == null)
                {
                    _logger.LogWarning($"❌ الشجرة {familyTreeId} غير موجودة أو لا تتبع للمستخدم {userId}");
                    TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation($"✅ الشجرة موجودة: {tree.Name}");

                // جلب الأشخاص مع العلاقات
                var persons = await _context.Persons
                    .Include(p => p.Occupation)
                    .Include(p => p.Country)
                    .Include(p => p.Father)
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .OrderBy(p => p.FirstName)
                    .ThenBy(p => p.FatherName)
                    .ToListAsync();

                _logger.LogInformation($"✅ تم تحميل {persons.Count} فرد للشجرة {familyTreeId}");

                // تسجيل تفاصيل كل فرد
                foreach (var person in persons)
                {
                    _logger.LogInformation($"   - الفرد: {person.Id} | {person.FullName} | الجنس: {person.Gender}");
                }

                ViewBag.FamilyTreeId = familyTreeId;
                ViewBag.FamilyTreeName = tree.Name;

                return View(persons);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ خطأ في تحميل قائمة الأشخاص للشجرة {familyTreeId}");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل قائمة الأشخاص";
                return View(new List<Person>());
            }
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