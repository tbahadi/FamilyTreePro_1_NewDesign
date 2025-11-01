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

        // دالة مساعدة محسنة لبناء الاسم الكامل مع معالجة المؤسس
        private string GetFullName(Person person)
        {
            if (person == null) return "غير معروف";

            try
            {
                var names = new List<string>();

                // الاسم الأول مطلوب دائماً
                if (!string.IsNullOrEmpty(person.FirstName))
                    names.Add(person.FirstName.Trim());

                // ⭐⭐ الإصلاح: معاملة جميع الأفراد بنفس الطريقة ⭐⭐
                // نعرض فقط الحقول التي تحتوي على بيانات فعلية

                if (!string.IsNullOrEmpty(person.FatherName) && person.FatherName != "غير معروف")
                    names.Add(person.FatherName.Trim());

                if (!string.IsNullOrEmpty(person.GrandFatherName) && person.GrandFatherName != "غير معروف")
                    names.Add(person.GrandFatherName.Trim());

                if (!string.IsNullOrEmpty(person.LastName) && person.LastName != "غير معروف")
                    names.Add(person.LastName.Trim());

                return names.Any() ? string.Join(" ", names) : "غير معروف";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في دالة GetFullName للشخص {PersonId}", person?.Id);
                return "غير معروف";
            }
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

            var tree = await _context.FamilyTrees
                .FirstOrDefaultAsync(ft => ft.Id == familyTreeId && ft.UserId == userId);
            if (tree == null)
            {
                TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                return RedirectToAction("Index", "Home");
            }

            // التحقق من وجود مؤسس في هذه الشجرة
            var hasFounder = await _context.Persons
                .AnyAsync(p => p.FamilyTreeId == familyTreeId && p.IsFounder);
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
                var father = await _context.Persons.FindAsync(fatherId.Value);
                ViewBag.FatherName = father != null ? GetFullName(father) : "غير معروف";
                ViewBag.IsAddingChild = true;

                // عند إضافة ابن، لا يمكن أن يكون مؤسساً
                viewModel.IsFounder = false;
                ViewBag.CanBeFounder = false;
            }
            else
            {
                ViewBag.CanBeFounder = !hasFounder;
            }

            if (motherId.HasValue)
            {
                var mother = await _context.Persons.FindAsync(motherId.Value);
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
            if (userId == null) return RedirectToAction("Login", "Account");

            try
            {
                // ⭐⭐ الإصلاح: معالجة حقل Photo ⭐⭐
                viewModel.Photo = string.IsNullOrWhiteSpace(viewModel.Photo) ? "" : viewModel.Photo.Trim();

                // تنظيف باقي البيانات
                viewModel.FirstName = viewModel.FirstName?.Trim();
                viewModel.FatherName = viewModel.FatherName?.Trim();
                viewModel.GrandFatherName = viewModel.GrandFatherName?.Trim(); // ⭐ سيصبح null إذا كان فارغاً
                viewModel.LastName = viewModel.LastName?.Trim();
                viewModel.Nickname = viewModel.Nickname?.Trim();
                viewModel.City = viewModel.City?.Trim();
                viewModel.Notes = viewModel.Notes?.Trim();
                viewModel.AdditionReason = viewModel.AdditionReason?.Trim();

                // ⭐⭐ الإصلاح: معالجة ابن المؤسس ⭐⭐
                if (viewModel.FatherId.HasValue)
                {
                    var father = await _context.Persons.FindAsync(viewModel.FatherId.Value);
                    if (father != null && father.IsFounder)
                    {
                        viewModel.FatherName = father.FirstName;
                        // ⭐⭐ التغيير هنا: عدم إجبار قيمة الجد ⭐⭐
                        // viewModel.GrandFatherName = "غير معروف"; // ❌ هذا السطر هو المشكلة
                        viewModel.GrandFatherName = viewModel.GrandFatherName; // ⭐ الاحتفاظ بالقيمة المدخلة
                        viewModel.LastName = father.LastName ?? "غير معروف";
                        viewModel.IsFounder = false;
                    }
                }

                if (viewModel.IsFounder)
                {
                    viewModel.FatherName = string.IsNullOrWhiteSpace(viewModel.FatherName) ? null : viewModel.FatherName;
                    viewModel.GrandFatherName = string.IsNullOrWhiteSpace(viewModel.GrandFatherName) ? null : viewModel.GrandFatherName;
                    viewModel.LastName = string.IsNullOrWhiteSpace(viewModel.LastName) ? null : viewModel.LastName;
                    viewModel.FatherId = null;
                    viewModel.MotherId = null;
                }

                // إزالة التحقق من الحقول غير الأساسية
                ModelState.Remove("FatherName");
                ModelState.Remove("GrandFatherName");
                ModelState.Remove("LastName");
                ModelState.Remove("Nickname");
                ModelState.Remove("City");
                ModelState.Remove("Notes");
                ModelState.Remove("Photo");
                ModelState.Remove("AdditionReason");
                ModelState.Remove("IsFounder");

                if (!ModelState.IsValid)
                {
                    await RepopulateViewBags(viewModel.FamilyTreeId);
                    return View(viewModel);
                }

                // ⭐⭐ إنشاء الشخص مع معالجة الحقول الفارغة ⭐⭐
                var person = new Person
                {
                    FirstName = viewModel.FirstName,
                    FatherName = viewModel.FatherName,
                    GrandFatherName = viewModel.GrandFatherName, // ⭐ الآن سيحتفظ بالقيمة المدخلة (قد تكون null)
                    LastName = viewModel.LastName,
                    Nickname = string.IsNullOrWhiteSpace(viewModel.Nickname) ? null : viewModel.Nickname, // ⭐ null بدلاً من "لا يوجد"
                    Gender = viewModel.Gender,
                    BirthDate = viewModel.BirthDate,
                    DeathDate = viewModel.DeathDate,
                    OccupationId = viewModel.OccupationId,
                    CountryId = viewModel.CountryId,
                    City = string.IsNullOrWhiteSpace(viewModel.City) ? null : viewModel.City, // ⭐ null بدلاً من "غير محدد"
                    Notes = string.IsNullOrWhiteSpace(viewModel.Notes) ? null : viewModel.Notes, // ⭐ null بدلاً من "لا يوجد"
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

                _context.Persons.Add(person);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"تم إضافة {GetFullName(person)} بنجاح!";
                return RedirectToAction("Index", new { familyTreeId = viewModel.FamilyTreeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إضافة الفرد");
                TempData["ErrorMessage"] = $"حدث خطأ أثناء الإضافة: {ex.Message}";
                await RepopulateViewBags(viewModel.FamilyTreeId);
                return View(viewModel);
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

                // ⭐⭐ معالجة خاصة للمؤسس ⭐⭐
                if (person.IsFounder)
                {
                    // للمؤسس: السماح بالقيم الفارغة (ستصبح NULL)
                    viewModel.FatherName = string.IsNullOrWhiteSpace(viewModel.FatherName) ? null : viewModel.FatherName;
                    viewModel.GrandFatherName = string.IsNullOrWhiteSpace(viewModel.GrandFatherName) ? null : viewModel.GrandFatherName;
                    viewModel.LastName = string.IsNullOrWhiteSpace(viewModel.LastName) ? null : viewModel.LastName;

                    // إزالة FatherId و MotherId للمؤسس
                    viewModel.FatherId = null;
                    viewModel.MotherId = null;

                    _logger.LogInformation("⭐ معالجة بيانات المؤسس في التعديل");
                }
                else
                {
                    // لغير المؤسس: استخدام "غير معروف" للحقول الفارغة
                    viewModel.FatherName = string.IsNullOrWhiteSpace(viewModel.FatherName) ? "غير معروف" : viewModel.FatherName;
                    viewModel.GrandFatherName = string.IsNullOrWhiteSpace(viewModel.GrandFatherName) ? "غير معروف" : viewModel.GrandFatherName;
                    viewModel.LastName = string.IsNullOrWhiteSpace(viewModel.LastName) ? "غير معروف" : viewModel.LastName;
                }

                // تحديث البيانات
                person.FirstName = viewModel.FirstName?.Trim();
                person.FatherName = viewModel.FatherName;
                person.GrandFatherName = viewModel.GrandFatherName;
                person.LastName = viewModel.LastName;
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

                // ⭐⭐ التحقق من المؤسس ⭐⭐
                if (person.IsFounder)
                {
                    ViewBag.CanDelete = false;
                    ViewBag.IsFounder = true;
                    ViewBag.ErrorMessage = "لا يمكن حذف المؤسس لأنه أساس الشجرة العائلية.";
                }
                else if (person.Children.Any())
                {
                    ViewBag.CanDelete = false;
                    ViewBag.IsFounder = false;
                    ViewBag.ChildrenCount = person.Children.Count;
                    ViewBag.ErrorMessage = $"لا يمكن حذف الفرد لأنه لديه {person.Children.Count} ابن/أبناء. يرجى حذف الأبناء أولاً.";
                }
                else
                {
                    ViewBag.CanDelete = true;
                    ViewBag.IsFounder = false;
                }

                ViewBag.FullName = GetFullName(person);
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

                // ⭐⭐ منع حذف المؤسس ⭐⭐
                if (person.IsFounder)
                {
                    TempData["ErrorMessage"] = "لا يمكن حذف المؤسس لأنه أساس الشجرة العائلية.";
                    return RedirectToAction("Delete", new { id = id });
                }

                var familyTreeId = person.FamilyTreeId;
                var personName = GetFullName(person);

                if (person.Children.Any())
                {
                    TempData["ErrorMessage"] = $"لا يمكن حذف الفرد {personName} لأنه لديه {person.Children.Count} ابن/أبناء. يرجى حذف الأبناء أولاً.";
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

        // أكشن لعرض قائمة الأشخاص في شجرة معينة
        public async Task<IActionResult> Index(int familyTreeId)
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

                // ⭐⭐ طريقة آمنة تماماً: تحميل البيانات بشكل منفصل ⭐⭐
                var persons = await _context.Persons
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .OrderByDescending(p => p.IsFounder) // المؤسس أولاً
                    .ThenBy(p => p.FirstName)
                    .ThenBy(p => p.FatherName)
                    .ToListAsync();

                // ⭐⭐ معالجة آمنة لجميع الحقول ⭐⭐
                var safePersons = persons.Select(p => new Person
                {
                    Id = p.Id,
                    FirstName = p.FirstName ?? "غير معروف",
                    FatherName = p.IsFounder ? p.FatherName : (p.FatherName ?? "غير معروف"),
                    GrandFatherName = p.IsFounder ? p.GrandFatherName : (p.GrandFatherName ?? "غير معروف"),
                    LastName = p.IsFounder ? p.LastName : (p.LastName ?? "غير معروف"),
                    Nickname = p.Nickname ?? "لا يوجد",
                    IsOriginalRecord = p.IsOriginalRecord,
                    IsConnectionPoint = p.IsConnectionPoint,
                    IsFounder = p.IsFounder,
                    OriginalTreeId = p.OriginalTreeId,
                    Gender = p.Gender ?? "Male",
                    BirthDate = p.BirthDate,
                    DeathDate = p.DeathDate,
                    City = p.City ?? "غير محدد",
                    Photo = p.Photo ?? "",
                    Notes = p.Notes ?? "لا يوجد",
                    AdditionReason = p.AdditionReason ?? "",
                    CreatedDate = p.CreatedDate,
                    LastUpdated = p.LastUpdated,
                    FamilyTreeId = p.FamilyTreeId,
                    OccupationId = p.OccupationId,
                    CountryId = p.CountryId,
                    FatherId = p.FatherId,
                    MotherId = p.MotherId
                }).ToList();

                ViewBag.FamilyTreeId = familyTreeId;
                ViewBag.FamilyTreeName = tree.Name;

                return View(safePersons);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في تحميل قائمة الأشخاص للشجرة {familyTreeId}");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل قائمة الأشخاص";
                return View(new List<Person>());
            }
        }

        // أكشن تفاصيل الشخص - محسن
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

                // إضافة الاسم الكامل للعرض
                ViewBag.FullName = GetFullName(person);
                ViewBag.IsFounder = person.IsFounder;

                return View(person);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل تفاصيل الفرد");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل التفاصيل";
                return RedirectToAction("Index", "Home");
            }
        }



        // باقي الأكشنز (ProfessionalTree, FamilyTreeView, إلخ) تبقى كما هي
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
                    IsFounder = p.IsFounder, // إضافة حقل المؤسس
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

                // جلب البيانات الأساسية مع تضمين العلاقات
                var persons = await _context.Persons
                    .Include(p => p.Occupation)
                    .Include(p => p.Country)
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .ToListAsync();

                // تحويل البيانات مع إضافة الحقول المطلوبة
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
                    DeathDate = p.DeathDate.HasValue ? p.DeathDate.Value.ToString("yyyy-MM-dd") : null, // إضافة تاريخ الوفاة
                    City = p.City ?? "",
                    FatherId = p.FatherId,
                    MotherId = p.MotherId, // إضافة معرف الأم
                    IsConnectionPoint = p.IsConnectionPoint,
                    IsFounder = p.IsFounder, // إضافة حقل المؤسس
                    //IsAlive = p.IsAlive, // إضافة حقل على قيد الحياة
                    OccupationName = p.Occupation?.Name ?? "",
                    CountryName = p.Country?.Name ?? "",
                    //Email = p.Email ?? "", // إضافة البريد الإلكتروني
                    //Phone = p.Phone ?? "", // إضافة رقم الهاتف
                    Notes = p.Notes ?? "", // إضافة الملاحظات
                                           // إضافة الحقول الأخرى التي قد تحتاجها
                   // ProfilePicture = p.ProfilePicture ?? "",
                   // EducationLevel = p.EducationLevel ?? ""
                }).ToList();

                _logger.LogInformation($"🔍 بيانات الشجرة الشاملة المحدثة: {personData.Count} فرد");

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
                _logger.LogError(ex, "خطأ في تحميل بيانات الشجرة الشاملة المحدثة");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل بيانات الشجرة الشاملة المحدثة";
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

                // تحميل الآباء المحتملين (لا يشمل المؤسس الحالي إذا كان في التعديل)
                var potentialFathersQuery = _context.Persons
                    .Where(p => p.FamilyTreeId == familyTreeId && p.Gender == "Male");

                if (currentPersonId.HasValue)
                {
                    potentialFathersQuery = potentialFathersQuery.Where(p => p.Id != currentPersonId.Value);
                }

                var potentialFathers = await potentialFathersQuery.ToListAsync();
                ViewBag.PotentialFathers = potentialFathers;
                _logger.LogInformation($"✅ تم تحميل {potentialFathers.Count} أب محتمل");

                // تحميل الأمهات المحتملات (لا تشمل المؤسس الحالي إذا كان في التعديل)
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
                var persons = await _context.Persons
                    .Where(p => p.IsFounder)
                    .ToListAsync();

                _logger.LogInformation($"🔍 عدد المهن في قاعدة البيانات: {occupations.Count}");
                _logger.LogInformation($"🔍 عدد الدول في قاعدة البيانات: {countries.Count}");
                _logger.LogInformation($"🔍 عدد المؤسسين في قاعدة البيانات: {persons.Count}");

                foreach (var founder in persons)
                {
                    _logger.LogInformation($"   - مؤسس: {GetFullName(founder)} (ID: {founder.Id}, الشجرة: {founder.FamilyTreeId})");
                }

                return Json(new
                {
                    OccupationsCount = occupations.Count,
                    CountriesCount = countries.Count,
                    FoundersCount = persons.Count,
                    Occupations = occupations,
                    Countries = countries,
                    Founders = persons.Select(f => new {
                        id = f.Id,
                        name = GetFullName(f),
                        treeId = f.FamilyTreeId
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ في التحقق من البيانات");
                return Json(new { error = ex.Message });
            }
        }

        // دالة مساعدة للتحقق من وجود مؤسس في الشجرة
        private async Task<bool> HasFounderInTree(int familyTreeId)
        {
            return await _context.Persons
                .AnyAsync(p => p.FamilyTreeId == familyTreeId && p.IsFounder);
        }

        // دالة مساعدة للحصول على المؤسس
        private async Task<Person> GetFounder(int familyTreeId)
        {
            return await _context.Persons
                .FirstOrDefaultAsync(p => p.FamilyTreeId == familyTreeId && p.IsFounder);
        }
    }
}