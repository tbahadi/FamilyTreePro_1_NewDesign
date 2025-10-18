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
        private string GetFullName(Person person)
        {
            if (person == null) return "غير معروف";

            var names = new List<string>();

            if (!string.IsNullOrEmpty(person.FirstName))
                names.Add(person.FirstName);

            if (!string.IsNullOrEmpty(person.FatherName))
                names.Add(person.FatherName);

            if (!string.IsNullOrEmpty(person.GrandFatherName))
                names.Add(person.GrandFatherName);

            if (!string.IsNullOrEmpty(person.LastName))
                names.Add(person.LastName);

            return string.Join(" ", names);
        }

        // GET: إضافة فرد جديد
        [HttpGet]
        public IActionResult Create(int familyTreeId, int? fatherId = null, int? motherId = null)
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

            var viewModel = new CreatePersonViewModel
            {
                FamilyTreeId = familyTreeId,
                FatherId = fatherId,
                MotherId = motherId
            };

            RepopulateViewBags(familyTreeId).Wait();

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

            _logger.LogInformation($"🔍 بيانات النموذج المستلمة:");
            _logger.LogInformation($"   - الاسم: {viewModel.FirstName}");
            _logger.LogInformation($"   - اسم الأب: {viewModel.FatherName}");
            _logger.LogInformation($"   - اسم الجد: {viewModel.GrandFatherName}");
            _logger.LogInformation($"   - العائلة: {viewModel.LastName}");
            _logger.LogInformation($"   - الجنس: {viewModel.Gender}");
            _logger.LogInformation($"   - الشجرة: {viewModel.FamilyTreeId}");

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
                var person = new Person
                {
                    FirstName = viewModel.FirstName?.Trim(),
                    FatherName = viewModel.FatherName?.Trim(),
                    GrandFatherName = viewModel.GrandFatherName?.Trim(),
                    LastName = viewModel.LastName?.Trim(),
                    Nickname = viewModel.Nickname?.Trim(),
                    Gender = viewModel.Gender,
                    BirthDate = viewModel.BirthDate,
                    DeathDate = viewModel.DeathDate,
                    OccupationId = viewModel.OccupationId,
                    CountryId = viewModel.CountryId,
                    City = viewModel.City?.Trim(),
                    Notes = viewModel.Notes?.Trim(),
                    FamilyTreeId = viewModel.FamilyTreeId,
                    FatherId = viewModel.FatherId,
                    MotherId = viewModel.MotherId,
                    AdditionReason = viewModel.AdditionReason,
                    Photo = viewModel.Photo ?? string.Empty,
                    IsOriginalRecord = true,
                    IsConnectionPoint = false,
                    CreatedDate = DateTime.Now,
                    LastUpdated = DateTime.Now
                };

                var fullName = GetFullName(person);
                _logger.LogInformation($"💾 محاولة حفظ الشخص: {fullName}");

                _context.Persons.Add(person);
                int recordsAffected = await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ تم حفظ الشخص بنجاح! السجلات المتأثرة: {recordsAffected}, الرقم: {person.Id}");

                TempData["SuccessMessage"] = $"تم إضافة الفرد {fullName} بنجاح!";
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

            await RepopulateViewBags(viewModel.FamilyTreeId);
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

        // الشجرة الهرمية المتقدمة
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

                // جلب البيانات مع تضمين العلاقات
                var persons = await _context.Persons
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .Select(p => new
                    {
                        Id = p.Id,
                        FirstName = p.FirstName,
                        FatherName = p.FatherName,
                        GrandFatherName = p.GrandFatherName,
                        LastName = p.LastName,
                        Nickname = p.Nickname,
                        FullName = GetFullName(p),
                        Gender = p.Gender,
                        BirthDate = p.BirthDate,
                        City = p.City,
                        FatherId = p.FatherId,
                        IsConnectionPoint = p.IsConnectionPoint,
                        OccupationName = p.Occupation != null ? p.Occupation.Name : null,
                        OccupationId = p.OccupationId,
                        CountryName = p.Country != null ? p.Country.Name : null,
                        CountryId = p.CountryId
                    })
                    .ToListAsync();

                _logger.LogInformation($"🔍 بيانات الشجرة المتقدمة: {persons.Count} فرد");

                ViewBag.FamilyTreeId = familyTreeId;
                ViewBag.FamilyTreeName = tree.Name;
                ViewBag.PersonsCount = persons.Count;

                var jsonOptions = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                ViewBag.PersonsJson = JsonSerializer.Serialize(persons, jsonOptions);

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

                var persons = await _context.Persons
                    .Include(p => p.Father)
                    .Include(p => p.Children)
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .ToListAsync();

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

        // دالة مساعدة لإعادة تعبئة ViewBags
        private async Task RepopulateViewBags(int familyTreeId, int? currentPersonId = null)
        {
            ViewBag.Occupations = await _context.Occupations.ToListAsync();
            ViewBag.Countries = await _context.Countries.ToListAsync();

            var potentialFathersQuery = _context.Persons
                .Where(p => p.FamilyTreeId == familyTreeId && p.Gender == "Male");

            if (currentPersonId.HasValue)
            {
                potentialFathersQuery = potentialFathersQuery.Where(p => p.Id != currentPersonId.Value);
            }

            ViewBag.PotentialFathers = await potentialFathersQuery.ToListAsync();

            var potentialMothersQuery = _context.Persons
                .Where(p => p.FamilyTreeId == familyTreeId && p.Gender == "Female");

            if (currentPersonId.HasValue)
            {
                potentialMothersQuery = potentialMothersQuery.Where(p => p.Id != currentPersonId.Value);
            }

            ViewBag.PotentialMothers = await potentialMothersQuery.ToListAsync();
        }
    }
}