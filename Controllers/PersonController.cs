using FamilyTreePro.Models;
using FamilyTreePro.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace FamilyTreePro.Controllers
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

        // دالة مساعدة لبناء الاسم الكامل
        // دالة مساعدة لبناء الاسم الكامل
        private string GetFullName(Person person)
        {
            if (person == null) return "غير معروف";

            var names = new List<string>();

            if (!string.IsNullOrEmpty(person.FirstName))
                names.Add(person.FirstName.Trim());

            if (!string.IsNullOrEmpty(person.FatherName))
                names.Add(person.FatherName.Trim());

            if (!string.IsNullOrEmpty(person.GrandFatherName))
                names.Add(person.GrandFatherName.Trim());

            if (!string.IsNullOrEmpty(person.LastName))
                names.Add(person.LastName.Trim());

            return names.Any() ? string.Join(" ", names) : "غير معروف";
        }

        // GET: إضافة فرد جديد
        [HttpGet]
        public async Task<IActionResult> Create(int familyTreeId, int? fatherId = null, int? motherId = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var tree = _context.FamilyTrees.FirstOrDefault(ft => ft.Id == familyTreeId && ft.UserId == userId);
            if (tree == null)
            {
                TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                return RedirectToAction("Index", "Home");
            }

            // التحقق من وجود مؤسس في هذه الشجرة
            var hasFounder = _context.Persons.Any(p => p.FamilyTreeId == familyTreeId && p.IsFounder);
            ViewBag.HasFounder = hasFounder;

            var viewModel = new CreatePersonViewModel
            {
                FamilyTreeId = familyTreeId,
                FatherId = fatherId,
                MotherId = motherId,
                IsFounder = false // قيمة افتراضية
            };

            await RepopulateViewBags(familyTreeId);

            if (fatherId.HasValue)
            {
                var father = _context.Persons.Find(fatherId.Value);
                ViewBag.FatherName = father != null ? GetFullName(father) : "غير معروف";
                ViewBag.IsAddingChild = true;
            }

            if (motherId.HasValue)
            {
                var mother = _context.Persons.Find(motherId.Value);
                ViewBag.MotherName = mother != null ? GetFullName(mother) : "غير معروف";
            }

            return View(viewModel);
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

            // تنظيف البيانات من المسافات الزائدة
            if (!string.IsNullOrEmpty(viewModel.FirstName))
                viewModel.FirstName = viewModel.FirstName.Trim();
            if (!string.IsNullOrEmpty(viewModel.FatherName))
                viewModel.FatherName = viewModel.FatherName.Trim();
            if (!string.IsNullOrEmpty(viewModel.GrandFatherName))
                viewModel.GrandFatherName = viewModel.GrandFatherName.Trim();
            if (!string.IsNullOrEmpty(viewModel.LastName))
                viewModel.LastName = viewModel.LastName.Trim();
            if (!string.IsNullOrEmpty(viewModel.Nickname))
                viewModel.Nickname = viewModel.Nickname.Trim();
            if (!string.IsNullOrEmpty(viewModel.City))
                viewModel.City = viewModel.City.Trim();
            if (!string.IsNullOrEmpty(viewModel.Notes))
                viewModel.Notes = viewModel.Notes.Trim();
            if (!string.IsNullOrEmpty(viewModel.AdditionReason))
                viewModel.AdditionReason = viewModel.AdditionReason.Trim();

            // ⭐⭐ الإصلاحات - تعيين القيم الافتراضية للحقول المطلوبة ⭐⭐
            if (string.IsNullOrWhiteSpace(viewModel.Nickname)) viewModel.Nickname = "لا يوجد";
            if (string.IsNullOrWhiteSpace(viewModel.City)) viewModel.City = "غير محدد";
            if (string.IsNullOrWhiteSpace(viewModel.Notes)) viewModel.Notes = "لا يوجد";
            if (string.IsNullOrWhiteSpace(viewModel.Photo)) viewModel.Photo = "";
            if (string.IsNullOrWhiteSpace(viewModel.AdditionReason)) viewModel.AdditionReason = "";

            // ⭐⭐ الإصلاح الجذري: إزالة أخطاء التحقق للحقول الغير مطلوبة ⭐⭐
            ModelState.Remove("Nickname");
            ModelState.Remove("City");
            ModelState.Remove("Notes");
            ModelState.Remove("Photo");
            ModelState.Remove("AdditionReason");
            ModelState.Remove("IsFounder");
            ModelState.Remove("AddType");

            // ⭐⭐ إزالة أخطاء التحقق للحقول التي لم تعد مطلوبة ⭐⭐
            ModelState.Remove("FatherName");
            ModelState.Remove("GrandFatherName");
            ModelState.Remove("LastName");

            _logger.LogInformation($"🔍 بيانات النموذج المستلمة بعد التنظيف:");
            _logger.LogInformation($"   - الاسم: '{viewModel.FirstName}'");
            _logger.LogInformation($"   - اسم الأب: '{viewModel.FatherName}'");
            _logger.LogInformation($"   - اسم الجد: '{viewModel.GrandFatherName}'");
            _logger.LogInformation($"   - العائلة: '{viewModel.LastName}'");
            _logger.LogInformation($"   - اللقب: '{viewModel.Nickname}'");
            _logger.LogInformation($"   - الجنس: '{viewModel.Gender}'");
            _logger.LogInformation($"   - الشجرة: {viewModel.FamilyTreeId}");
            _logger.LogInformation($"   - المدينة: '{viewModel.City}'");
            _logger.LogInformation($"   - الملاحظات: '{viewModel.Notes}'");
            _logger.LogInformation($"   - مؤسس: '{viewModel.IsFounder}'");
            _logger.LogInformation($"   - نوع الإضافة: '{viewModel.AddType}'");

            // ⭐⭐ التحقق اليدوي المحسن من الحقول المطلوبة فقط ⭐⭐
            bool hasErrors = false;

            if (string.IsNullOrWhiteSpace(viewModel.FirstName))
            {
                ModelState.AddModelError("FirstName", "الاسم الأول مطلوب");
                hasErrors = true;
            }

            if (string.IsNullOrWhiteSpace(viewModel.Gender))
            {
                ModelState.AddModelError("Gender", "الجنس مطلوب");
                hasErrors = true;
            }

            // ⭐⭐ التحقق من المؤسس ⭐⭐
            if (viewModel.IsFounder)
            {
                // تحقق إذا كان هناك مؤسس بالفعل في هذه الشجرة
                var existingFounder = await _context.Persons
                    .FirstOrDefaultAsync(p => p.FamilyTreeId == viewModel.FamilyTreeId && p.IsFounder);
                if (existingFounder != null)
                {
                    ModelState.AddModelError("IsFounder", "يوجد مؤسس بالفعل في الشجرة العائلية. لا يمكن إضافة أكثر من مؤسس واحد.");
                    hasErrors = true;
                    _logger.LogWarning($"❌ محاولة إضافة مؤسس جديد مع وجود مؤسس موجود: {existingFounder.Id}");
                }
            }

            if (!ModelState.IsValid || hasErrors)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning($"❌ أخطاء التحقق: {string.Join(", ", errors)}");

                // تسجيل تفاصيل أكثر عن أخطاء التحقق
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    if (state.Errors.Any())
                    {
                        _logger.LogWarning($"   - {key}: {string.Join(", ", state.Errors.Select(e => e.ErrorMessage))}");
                    }
                }

                TempData["ErrorMessage"] = "البيانات غير صالحة. يرجى تصحيح الأخطاء أدناه.";

                await RepopulateViewBags(viewModel.FamilyTreeId);
                // ⭐⭐ إعادة تحميل حالة وجود مؤسس للعرض ⭐⭐
                var hasFounder = _context.Persons.Any(p => p.FamilyTreeId == viewModel.FamilyTreeId && p.IsFounder);
                ViewBag.HasFounder = hasFounder;
                return View(viewModel);
            }

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
                // ⭐⭐ تعيين القيم الافتراضية للحقول الاختيارية ⭐⭐
                if (string.IsNullOrWhiteSpace(viewModel.FatherName)) viewModel.FatherName = "غير معروف";
                if (string.IsNullOrWhiteSpace(viewModel.GrandFatherName)) viewModel.GrandFatherName = "غير معروف";
                if (string.IsNullOrWhiteSpace(viewModel.LastName)) viewModel.LastName = "غير معروف";

                var person = new Person
                {
                    FirstName = viewModel.FirstName,
                    FatherName = viewModel.FatherName,
                    GrandFatherName = viewModel.GrandFatherName,
                    LastName = viewModel.LastName,
                    Nickname = viewModel.Nickname,
                    Gender = viewModel.Gender,
                    BirthDate = viewModel.BirthDate,
                    DeathDate = viewModel.DeathDate,
                    OccupationId = viewModel.OccupationId,
                    CountryId = viewModel.CountryId,
                    City = viewModel.City,
                    Notes = viewModel.Notes,
                    FamilyTreeId = viewModel.FamilyTreeId,
                    FatherId = viewModel.FatherId,
                    MotherId = viewModel.MotherId,
                    AdditionReason = viewModel.AdditionReason,
                    Photo = viewModel.Photo,
                    IsFounder = viewModel.IsFounder,
                    IsOriginalRecord = true,
                    IsConnectionPoint = false,
                    CreatedDate = DateTime.Now,
                    LastUpdated = DateTime.Now
                };

                var fullName = GetFullName(person);
                _logger.LogInformation($"💾 محاولة حفظ الشخص: {fullName}، مؤسس: {person.IsFounder}");

                _context.Persons.Add(person);
                int recordsAffected = await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ تم حفظ الشخص بنجاح! السجلات المتأثرة: {recordsAffected}, الرقم: {person.Id}");

                // إذا كان طلب AJAX، إرجاع JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        personName = fullName,
                        isFounder = person.IsFounder
                    });
                }

                if (person.IsFounder)
                {
                    TempData["SuccessMessage"] = $"تم إضافة المؤسس {fullName} بنجاح!";
                }
                else
                {
                    TempData["SuccessMessage"] = $"تم إضافة الفرد {fullName} بنجاح!";
                }

                return RedirectToAction("Index", new { familyTreeId = viewModel.FamilyTreeId });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "❌ خطأ في قاعدة البيانات أثناء إضافة الفرد");
                _logger.LogError($"تفاصيل الخطأ الداخلية: {dbEx.InnerException?.Message}");

                // إذا كان طلب AJAX، إرجاع JSON للخطأ
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = false,
                        message = "حدث خطأ في قاعدة البيانات أثناء الحفظ"
                    });
                }

                TempData["ErrorMessage"] = "حدث خطأ في قاعدة البيانات. قد تكون البيانات مكررة أو غير صالحة.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ غير متوقع أثناء إضافة الفرد");

                // إذا كان طلب AJAX، إرجاع JSON للخطأ
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = false,
                        message = "حدث خطأ غير متوقع أثناء الحفظ"
                    });
                }

                TempData["ErrorMessage"] = $"حدث خطأ غير متوقع: {ex.Message}";
            }

            await RepopulateViewBags(viewModel.FamilyTreeId);
            // ⭐⭐ إعادة تحميل حالة وجود مؤسس للعرض ⭐⭐
            var hasFounderInTree = _context.Persons.Any(p => p.FamilyTreeId == viewModel.FamilyTreeId && p.IsFounder);
            ViewBag.HasFounder = hasFounderInTree;
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
                    DeathDate = person.DeathDate,
                    OccupationId = person.OccupationId,
                    CountryId = person.CountryId,
                    City = person.City,
                    Notes = person.Notes,
                    Photo = person.Photo,
                    AdditionReason = person.AdditionReason,
                    IsOriginalRecord = person.IsOriginalRecord,
                    IsConnectionPoint = person.IsConnectionPoint,
                    FamilyTreeId = person.FamilyTreeId,
                    FatherId = person.FatherId,
                    MotherId = person.MotherId,
                    CreatedDate = person.CreatedDate,
                    LastUpdated = person.LastUpdated
                };

                await RepopulateViewBags(person.FamilyTreeId, person.Id);
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
                await RepopulateViewBags(viewModel.FamilyTreeId, viewModel.Id);
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
                person.DeathDate = viewModel.DeathDate;
                person.OccupationId = viewModel.OccupationId;
                person.CountryId = viewModel.CountryId;
                person.City = viewModel.City?.Trim();
                person.Notes = viewModel.Notes?.Trim();
                person.FatherId = viewModel.FatherId;
                person.MotherId = viewModel.MotherId;
                person.AdditionReason = viewModel.AdditionReason;
                person.Photo = viewModel.Photo;
                person.IsOriginalRecord = viewModel.IsOriginalRecord;
                person.IsConnectionPoint = viewModel.IsConnectionPoint;
                person.LastUpdated = DateTime.Now;

                _context.Persons.Update(person);
                int recordsAffected = await _context.SaveChangesAsync();

                var fullName = GetFullName(person);
                _logger.LogInformation($"✅ تم تعديل الفرد بنجاح! السجلات المتأثرة: {recordsAffected}");

                TempData["SuccessMessage"] = $"تم تعديل الفرد {fullName} بنجاح!";
                return RedirectToAction("Details", new { id = viewModel.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ أثناء تعديل الفرد");
                TempData["ErrorMessage"] = $"حدث خطأ أثناء التعديل: {ex.Message}";

                await RepopulateViewBags(viewModel.FamilyTreeId, viewModel.Id);
                return View(viewModel);
            }
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
                var personName = GetFullName(person);

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

        // أكشن للتحقق من البيانات (للت debug فقط)
        public async Task<IActionResult> DebugTreeData(int familyTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { error = "غير مسجل" });

            var tree = await _context.FamilyTrees
                .FirstOrDefaultAsync(ft => ft.Id == familyTreeId && ft.UserId == userId);

            if (tree == null) return Json(new { error = "شجرة غير موجودة" });

            var persons = await _context.Persons
                .Where(p => p.FamilyTreeId == familyTreeId)
                .ToListAsync();

            return Json(new
            {
                treeName = tree.Name,
                personsCount = persons.Count,
                persons = persons.Select(p => new {
                    id = p.Id,
                    name = GetFullName(p),
                    fatherId = p.FatherId
                })
            });
        }
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
                var tree = await _context.FamilyTrees
                    .FirstOrDefaultAsync(ft => ft.Id == familyTreeId && ft.UserId == userId);

                if (tree == null)
                {
                    _logger.LogWarning($"❌ الشجرة {familyTreeId} غير موجودة أو لا تتبع للمستخدم {userId}");
                    TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation($"✅ الشجرة موجودة: {tree.Name}");

                var persons = await _context.Persons
                    .Include(p => p.Occupation)
                    .Include(p => p.Country)
                    .Include(p => p.Father)
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .OrderBy(p => p.FirstName)
                    .ThenBy(p => p.FatherName)
                    .ToListAsync();

                _logger.LogInformation($"✅ تم تحميل {persons.Count} فرد للشجرة {familyTreeId}");

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

        // أكشن تفاصيل الشخص
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
                    .Include(p => p.Mother)
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
        public async Task<IActionResult> EnhancedTree(int familyTreeId)
        {
            var familyTree = await _context.FamilyTrees
                .FirstOrDefaultAsync(ft => ft.Id == familyTreeId);

            if (familyTree == null)
            {
                return NotFound();
            }

            var allPersons = await _context.Persons
                .Include(p => p.Father)
                .Include(p => p.Mother)
                .Include(p => p.Country)
                .Include(p => p.Occupation)
                .Where(p => p.FamilyTreeId == familyTreeId)
                .ToListAsync();

            var rootPersons = allPersons.Where(p => p.FatherId == null).ToList();

            var viewModel = new FamilyTreeViewViewModel
            {
                FamilyTreeId = familyTreeId,
                FamilyTreeName = familyTree.Name,
                AllPersons = allPersons,
                RootPersons = rootPersons
            };

            return View(viewModel);
        }
        public async Task<IActionResult> CompareTrees(int familyTreeId)
        {
            var familyTree = await _context.FamilyTrees
                .FirstOrDefaultAsync(ft => ft.Id == familyTreeId);

            if (familyTree == null)
            {
                return NotFound();
            }

            var allPersons = await _context.Persons
                .Include(p => p.Father)
                .Include(p => p.Mother)
                .Include(p => p.Country)
                .Include(p => p.Occupation)
                .Where(p => p.FamilyTreeId == familyTreeId)
                .ToListAsync();

            var rootPersons = allPersons.Where(p => p.FatherId == null).ToList();

            var viewModel = new FamilyTreeViewViewModel
            {
                FamilyTreeId = familyTreeId,
                FamilyTreeName = familyTree.Name,
                AllPersons = allPersons,
                RootPersons = rootPersons
            };

            return View(viewModel);
        }


        public async Task<IActionResult> LeafTree(int familyTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var tree = await _context.FamilyTrees
                    .FirstOrDefaultAsync(ft => ft.Id == familyTreeId && ft.UserId == userId);

                if (tree == null)
                {
                    TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                    return RedirectToAction("Index", "Home");
                }

                // جلب البيانات الأساسية
                var persons = await _context.Persons
                    .Include(p => p.Occupation)
                    .Include(p => p.Country)
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .ToListAsync();

                // تحويل البيانات
                var personData = persons.Select(p => new
                {
                    Id = p.Id,
                    FirstName = p.FirstName ?? "",
                    FatherName = p.FatherName ?? "",
                    GrandFatherName = p.GrandFatherName ?? "",
                    LastName = p.LastName ?? "",
                    Nickname = p.Nickname ?? "",
                    FullName = GetFullName(p),
                    Gender = p.Gender ?? "Male",
                    BirthDate = p.BirthDate.HasValue ? p.BirthDate.Value.ToString("yyyy-MM-dd") : null,
                    City = p.City ?? "",
                    FatherId = p.FatherId,
                    IsConnectionPoint = p.IsConnectionPoint,
                    OccupationName = p.Occupation?.Name ?? "",
                    CountryName = p.Country?.Name ?? ""
                }).ToList();

                _logger.LogInformation($"🔍 بيانات الشجرةالورقة: {personData.Count} فرد");

                ViewBag.FamilyTreeId = familyTreeId;
                ViewBag.FamilyTreeName = tree.Name;
                ViewBag.PersonsCount = personData.Count;

                // إعداد JSON
                var jsonOptions = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false
                };

                ViewBag.PersonsJson = System.Text.Json.JsonSerializer.Serialize(personData, jsonOptions);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل بيانات الشجرةالورقة: ");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل بيانات الشجرةالورقة: ";
                return RedirectToAction("Index", "Home");
            }
        }

        //ComperhensiveTreeView
        public async Task<IActionResult> ComperhensiveTreeView(int familyTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var tree = await _context.FamilyTrees
                    .FirstOrDefaultAsync(ft => ft.Id == familyTreeId && ft.UserId == userId);

                if (tree == null)
                {
                    TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                    return RedirectToAction("Index", "Home");
                }

                // جلب البيانات الأساسية
                var persons = await _context.Persons
                    .Include(p => p.Occupation)
                    .Include(p => p.Country)
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .ToListAsync();

                // تحويل البيانات
                var personData = persons.Select(p => new
                {
                    Id = p.Id,
                    FirstName = p.FirstName ?? "",
                    FatherName = p.FatherName ?? "",
                    GrandFatherName = p.GrandFatherName ?? "",
                    LastName = p.LastName ?? "",
                    Nickname = p.Nickname ?? "",
                    FullName = GetFullName(p),
                    Gender = p.Gender ?? "Male",
                    BirthDate = p.BirthDate.HasValue ? p.BirthDate.Value.ToString("yyyy-MM-dd") : null,
                    City = p.City ?? "",
                    FatherId = p.FatherId,
                    IsConnectionPoint = p.IsConnectionPoint,
                    OccupationName = p.Occupation?.Name ?? "",
                    CountryName = p.Country?.Name ?? ""
                }).ToList();

                _logger.LogInformation($"🔍 بيانات الشجرة البسيطة الجديدة: {personData.Count} فرد");

                ViewBag.FamilyTreeId = familyTreeId;
                ViewBag.FamilyTreeName = tree.Name;
                ViewBag.PersonsCount = personData.Count;

                // إعداد JSON
                var jsonOptions = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false
                };

                ViewBag.PersonsJson = System.Text.Json.JsonSerializer.Serialize(personData, jsonOptions);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل بيانات الشجرة البسيطة الجديدة: ");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل بيانات الشجرة البسيطة الجديدة: ";
                return RedirectToAction("Index", "Home");
            }
        }
        public async Task<IActionResult> SamplePage(int familyTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var tree = await _context.FamilyTrees
                    .FirstOrDefaultAsync(ft => ft.Id == familyTreeId && ft.UserId == userId);

                if (tree == null)
                {
                    TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                    return RedirectToAction("Index", "Home");
                }

                // جلب البيانات الأساسية
                var persons = await _context.Persons
                    .Include(p => p.Occupation)
                    .Include(p => p.Country)
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .ToListAsync();

                // تحويل البيانات
                var personData = persons.Select(p => new
                {
                    Id = p.Id,
                    FirstName = p.FirstName ?? "",
                    FatherName = p.FatherName ?? "",
                    GrandFatherName = p.GrandFatherName ?? "",
                    LastName = p.LastName ?? "",
                    Nickname = p.Nickname ?? "",
                    FullName = GetFullName(p),
                    Gender = p.Gender ?? "Male",
                    BirthDate = p.BirthDate.HasValue ? p.BirthDate.Value.ToString("yyyy-MM-dd") : null,
                    City = p.City ?? "",
                    FatherId = p.FatherId,
                    IsConnectionPoint = p.IsConnectionPoint,
                    OccupationName = p.Occupation?.Name ?? "",
                    CountryName = p.Country?.Name ?? ""
                }).ToList();

                _logger.LogInformation($"🔍 بيانات الشجرة البسيطة الجديدة: {personData.Count} فرد");

                ViewBag.FamilyTreeId = familyTreeId;
                ViewBag.FamilyTreeName = tree.Name;
                ViewBag.PersonsCount = personData.Count;

                // إعداد JSON
                var jsonOptions = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false
                };

                ViewBag.PersonsJson = System.Text.Json.JsonSerializer.Serialize(personData, jsonOptions);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل بيانات الشجرة البسيطة الجديدة: ");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل بيانات الشجرة البسيطة الجديدة: ";
                return RedirectToAction("Index", "Home");
            }
        }
        // الشجرة الهرمية المتقدمة
        // الشجرة العائلية المتقدمة
        // الشجرة العائلية المتقدمة
        // الشجرة العائلية المتقدمة
        // في PersonController
        public async Task<IActionResult> ProfessionalTree(int familyTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var tree = await _context.FamilyTrees
                    .FirstOrDefaultAsync(ft => ft.Id == familyTreeId && ft.UserId == userId);

                if (tree == null)
                {
                    TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                    return RedirectToAction("Index", "Home");
                }

                // جلب البيانات الأساسية
                var persons = await _context.Persons
                    .Include(p => p.Occupation)
                    .Include(p => p.Country)
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .ToListAsync();

                // تحويل البيانات
                var personData = persons.Select(p => new
                {
                    Id = p.Id,
                    FirstName = p.FirstName ?? "",
                    FatherName = p.FatherName ?? "",
                    GrandFatherName = p.GrandFatherName ?? "",
                    LastName = p.LastName ?? "",
                    Nickname = p.Nickname ?? "",
                    FullName = GetFullName(p),
                    Gender = p.Gender ?? "Male",
                    BirthDate = p.BirthDate.HasValue ? p.BirthDate.Value.ToString("yyyy-MM-dd") : null,
                    City = p.City ?? "",
                    FatherId = p.FatherId,
                    IsConnectionPoint = p.IsConnectionPoint,
                    OccupationName = p.Occupation?.Name ?? "",
                    CountryName = p.Country?.Name ?? ""
                }).ToList();

                _logger.LogInformation($"🔍 بيانات الشجرة المتقدمة: {personData.Count} فرد");

                ViewBag.FamilyTreeId = familyTreeId;
                ViewBag.FamilyTreeName = tree.Name;
                ViewBag.PersonsCount = personData.Count;

                // إعداد JSON
                var jsonOptions = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false
                };

                ViewBag.PersonsJson = System.Text.Json.JsonSerializer.Serialize(personData, jsonOptions);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل الشجرة المتقدمة");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل الشجرة المتقدمة";
                return RedirectToAction("Index", "Home");
            }
        }

        // عرض الشجرة الهرمية
        // عرض الشجرة الهرمية
        // عرض الشجرة الهرمية
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
                var tree = await _context.FamilyTrees
                    .FirstOrDefaultAsync(ft => ft.Id == familyTreeId && ft.UserId == userId);

                if (tree == null)
                {
                    TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                    return RedirectToAction("Index", "Home");
                }

                // جلب جميع الأشخاص في الشجرة
                var persons = await _context.Persons
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .ToListAsync();

                // العثور على الأشخاص الجذر (بدون أب)
                var rootPersons = persons.Where(p => p.FatherId == null).ToList();

                // إنشاء الـ ViewModel
                var viewModel = new FamilyTreeViewViewModel
                {
                    FamilyTreeId = familyTreeId,
                    FamilyTreeName = tree.Name,
                    RootPersons = rootPersons,
                    AllPersons = persons
                };

                _logger.LogInformation($"تم تحميل {persons.Count} فرد للشجرة الهرمية، منهم {rootPersons.Count} جذر");

                // إرسال الـ ViewModel إلى الـ View
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل الشجرة الهرمية");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل الشجرة الهرمية";
                return RedirectToAction("Index", "Home");
            }
        }

        // دالة مساعدة لإعادة تعبئة ViewBags - التحديث المهم هنا
        private async Task RepopulateViewBags(int familyTreeId, int? currentPersonId = null)
        {
            try
            {
                _logger.LogInformation("🔍 جاري تحميل قوائم المهن والدول...");

                // تحميل المهن
                var occupations = await _context.Occupations.ToListAsync();
                ViewBag.Occupations = occupations;
                _logger.LogInformation($"✅ تم تحميل {occupations.Count} مهنة");

                // تحميل الدول
                var countries = await _context.Countries.ToListAsync();
                ViewBag.Countries = countries;
                _logger.LogInformation($"✅ تم تحميل {countries.Count} دولة");

                // تحميل الآباء المحتملين
                var potentialFathersQuery = _context.Persons
                    .Where(p => p.FamilyTreeId == familyTreeId && p.Gender == "Male");

                if (currentPersonId.HasValue)
                {
                    potentialFathersQuery = potentialFathersQuery.Where(p => p.Id != currentPersonId.Value);
                }

                var potentialFathers = await potentialFathersQuery.ToListAsync();
                ViewBag.PotentialFathers = potentialFathers;
                _logger.LogInformation($"✅ تم تحميل {potentialFathers.Count} أب محتمل");

                // تحميل الأمهات المحتملات
                var potentialMothersQuery = _context.Persons
                    .Where(p => p.FamilyTreeId == familyTreeId && p.Gender == "Female");

                if (currentPersonId.HasValue)
                {
                    potentialMothersQuery = potentialMothersQuery.Where(p => p.Id != currentPersonId.Value);
                }

                var potentialMothers = await potentialMothersQuery.ToListAsync();
                ViewBag.PotentialMothers = potentialMothers;
                _logger.LogInformation($"✅ تم تحميل {potentialMothers.Count} أم محتملة");

                _logger.LogInformation("🎯 تم تحميل جميع البيانات بنجاح");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ في تحميل البيانات للعرض");
                // تعيين قيم افتراضية فارغة لتجنب الأخطاء
                ViewBag.Occupations = new List<Occupation>();
                ViewBag.Countries = new List<Country>();
                ViewBag.PotentialFathers = new List<Person>();
                ViewBag.PotentialMothers = new List<Person>();
            }
        }

        // أكشن للتحقق من البيانات في قاعدة البيانات (للت debug فقط)
        public async Task<IActionResult> CheckDatabaseData()
        {
            try
            {
                var occupations = await _context.Occupations.ToListAsync();
                var countries = await _context.Countries.ToListAsync();

                _logger.LogInformation($"🔍 عدد المهن في قاعدة البيانات: {occupations.Count}");
                _logger.LogInformation($"🔍 عدد الدول في قاعدة البيانات: {countries.Count}");

                foreach (var occupation in occupations)
                {
                    _logger.LogInformation($"   - مهنة: {occupation.Name} (ID: {occupation.Id})");
                }

                foreach (var country in countries)
                {
                    _logger.LogInformation($"   - دولة: {country.Name} (ID: {country.Id})");
                }

                return Json(new
                {
                    OccupationsCount = occupations.Count,
                    CountriesCount = countries.Count,
                    Occupations = occupations,
                    Countries = countries
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ في التحقق من البيانات");
                return Json(new { error = ex.Message });
            }
        }
    }
}