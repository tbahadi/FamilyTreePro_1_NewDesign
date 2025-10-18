using FamilyTreePro.Models;
using FamilyTreePro.Services;
using FamilyTreePro.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FamilyTreePro.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(AppDbContext context, ILogger<HomeController> logger)
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

        // الصفحة الرئيسية - عرض جميع الشجرات للمستخدم
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var familyTrees = await _context.FamilyTrees
                    .Where(ft => ft.UserId == userId)
                    .OrderByDescending(ft => ft.CreatedDate)
                    .ToListAsync();

                _logger.LogInformation($"🔍 تحميل الشجرات للمستخدم {userId}: {familyTrees.Count} شجرة");

                // حساب عدد الأفراد في كل شجرة - طريقة محسنة
                var personCounts = new Dictionary<int, int>();
                foreach (var tree in familyTrees)
                {
                    var count = await _context.Persons
                        .Where(p => p.FamilyTreeId == tree.Id)
                        .CountAsync();

                    personCounts[tree.Id] = count;
                    _logger.LogInformation($"   - الشجرة {tree.Id}: {tree.Name} - {count} فرد");
                }

                ViewBag.PersonCounts = personCounts;

                // إذا لم يكن للمستخدم أي شجرات، نقترح إنشاء واحدة
                if (!familyTrees.Any())
                {
                    ViewBag.NoTrees = true;
                    _logger.LogInformation($"المستخدم {userId} لا يملك أي شجرات");
                }

                return View(familyTrees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ في تحميل الشجرات");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل الشجرات";
                return View(new List<FamilyTree>());
            }
        }

        public async Task<IActionResult> AdvancedFamilyTreeView(int familyTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var persons = await _context.Persons
                    .Where(p => p.FamilyTreeId == familyTreeId)
                    .Include(p => p.Occupation)
                    .ToListAsync();

                var familyTree = await _context.FamilyTrees.FindAsync(familyTreeId);

                ViewBag.FamilyTreeId = familyTreeId;
                ViewBag.FamilyTreeName = familyTree?.Name ?? "شجرة عائلية";
                ViewBag.PersonsCount = persons.Count;

                // تحويل آمن للبيانات إلى JSON
                try
                {
                    var personsData = persons.Select(p => new
                    {
                        id = p.Id,
                        fullName = GetFullName(p), // استخدام الدالة المساعدة بدلاً من الخاصية
                        gender = p.Gender,
                        birthDate = p.BirthDate?.ToString("yyyy-MM-dd"),
                        city = p.City,
                        occupationName = p.Occupation?.Name,
                        fatherId = p.FatherId,
                        isConnectionPoint = p.IsConnectionPoint // الآن موجودة
                    }).ToList();

                    ViewBag.PersonsJson = JsonSerializer.Serialize(personsData,
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                }
                catch (Exception ex)
                {
                    ViewBag.PersonsJson = "[]";
                    _logger.LogError(ex, "Error serializing persons data");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل الشجرة المتقدمة");
                TempData["ErrorMessage"] = "حدث خطأ في تحميل الشجرة المتقدمة";
                return RedirectToAction("Index");
            }
        }

        // ربط شجرة بأخرى - GET
        public async Task<IActionResult> ConnectTrees(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var currentTree = await _context.FamilyTrees
                .FirstOrDefaultAsync(ft => ft.Id == id && ft.UserId == userId);

            if (currentTree == null)
            {
                TempData["ErrorMessage"] = "الشجرة غير موجودة";
                return RedirectToAction(nameof(Index));
            }

            // الحصول على الشجرات الأخرى للمستخدم التي يمكن ربطها
            var availableTrees = await _context.FamilyTrees
                .Include(ft => ft.Persons)
                .Where(ft => ft.UserId == userId && ft.Id != id && ft.ParentTreeId == null)
                .ToListAsync();

            ViewBag.CurrentTree = currentTree;
            ViewBag.AvailableTrees = availableTrees;

            return View();
        }

        // ربط شجرة بأخرى - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConnectTrees(int treeId, int? parentTreeId, int? connectionPersonId,
            [FromServices] DataSyncService dataSyncService)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var tree = await _context.FamilyTrees
                .FirstOrDefaultAsync(ft => ft.Id == treeId && ft.UserId == userId);

            if (tree == null)
            {
                TempData["ErrorMessage"] = "الشجرة غير موجودة";
                return RedirectToAction(nameof(Index));
            }

            if (parentTreeId.HasValue)
            {
                try
                {
                    // نسخ البيانات إلى الشجرة الأم
                    bool copySuccess = await dataSyncService.CopyTreeData(treeId, parentTreeId.Value, connectionPersonId);

                    if (copySuccess)
                    {
                        TempData["SuccessMessage"] = "تم ربط الشجرات ونسخ البيانات بنجاح!";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "تم الربط ولكن حدث خطأ في نسخ البيانات";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ أثناء ربط الشجرات");
                    TempData["ErrorMessage"] = $"حدث خطأ أثناء الربط: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "يجب اختيار شجرة أم للربط";
            }

            return RedirectToAction(nameof(Index));
        }

        // حذف شجرة عائلية - GET
        public async Task<IActionResult> DeleteTree(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var tree = await _context.FamilyTrees
                .Include(ft => ft.Persons)
                .Include(ft => ft.ChildTrees)
                .FirstOrDefaultAsync(ft => ft.Id == id && ft.UserId == userId);

            if (tree == null)
            {
                TempData["ErrorMessage"] = "الشجرة غير موجودة";
                return RedirectToAction(nameof(Index));
            }

            // النظام الجديد: التحقق من أنواع الشجرات
            if (tree.ParentTreeId.HasValue)
            {
                // شجرة فرعية مرتبطة - يمكن حذفها (البيانات في الشجرة الأم)
                ViewBag.IsLinkedTree = true;
                ViewBag.ParentTreeId = tree.ParentTreeId;
                ViewBag.CanDelete = true;
            }
            else if (tree.ChildTrees.Any())
            {
                // شجرة أم تحتوي على شجرات فرعية
                ViewBag.HasChildTrees = true;
                ViewBag.ChildTreeCount = tree.ChildTrees.Count;
                ViewBag.CanDelete = false; // لا يمكن حذفها مباشرة
            }
            else if (tree.Persons.Any())
            {
                // شجرة مستقلة تحتوي على أفراد
                ViewBag.HasPersons = true;
                ViewBag.PersonCount = tree.Persons.Count;
                ViewBag.CanDelete = false; // لا يمكن حذفها مباشرة
            }
            else
            {
                // شجرة فارغة - يمكن حذفها
                ViewBag.CanDelete = true;
            }

            ViewBag.PersonCount = tree.Persons.Count;
            ViewBag.ChildTreeCount = tree.ChildTrees.Count;

            return View(tree);
        }

        [HttpPost, ActionName("DeleteTree")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTreeConfirmed(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var tree = await _context.FamilyTrees
                .Include(ft => ft.Persons)
                .FirstOrDefaultAsync(ft => ft.Id == id && ft.UserId == userId);

            if (tree != null)
            {
                try
                {
                    // إذا كانت الشجرة مربوطة بشجرة أم، نحذفها فقط دون حذف الأشخاص
                    // لأن الأشخاص موجودين في الشجرة الأم
                    if (tree.ParentTreeId.HasValue)
                    {
                        _context.FamilyTrees.Remove(tree);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "تم حذف الشجرة بنجاح! البيانات محفوظة في الشجرة الأم.";
                    }
                    else
                    {
                        // إذا كانت شجرة مستقلة، نحذفها مع أشخاصها
                        _context.FamilyTrees.Remove(tree);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "تم حذف الشجرة وجميع بياناتها بنجاح!";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ أثناء حذف الشجرة");
                    TempData["ErrorMessage"] = $"حدث خطأ أثناء الحذف: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // أكشن لفحص جميع البيانات (للتجربة فقط)
        public async Task<IActionResult> DebugData()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var trees = await _context.FamilyTrees
                    .Where(ft => ft.UserId == userId)
                    .ToListAsync();

                var persons = await _context.Persons
                    .Where(p => p.FamilyTree.UserId == userId)
                    .ToListAsync();

                ViewBag.Trees = trees;
                ViewBag.Persons = persons;
                ViewBag.UserId = userId;

                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"خطأ في فحص البيانات: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // أكشن لإنشاء شجرة افتراضية للمستخدم الجديد
        public async Task<IActionResult> CreateDefaultTree()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // التحقق إذا كان للمستخدم شجرة بالفعل
                var existingTree = await _context.FamilyTrees
                    .FirstOrDefaultAsync(ft => ft.UserId == userId);

                if (existingTree == null)
                {
                    // إنشاء شجرة افتراضية
                    var defaultTree = new FamilyTree
                    {
                        Name = "شجرتي العائلية",
                        Description = "الشجرة العائلية الرئيسية",
                        Color = "#007bff",
                        UserId = userId.Value,
                        CreatedDate = DateTime.Now
                    };

                    _context.FamilyTrees.Add(defaultTree);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تم إنشاء شجرة عائلية افتراضية لك!";
                }
                else
                {
                    TempData["InfoMessage"] = "لديك شجرة عائلية بالفعل";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء الشجرة الافتراضية");
                TempData["ErrorMessage"] = "حدث خطأ في إنشاء الشجرة الافتراضية";
                return RedirectToAction(nameof(Index));
            }
        }

        // إنشاء شجرة عائلية جديدة - GET
        public IActionResult CreateFamilyTree()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View(new CreateFamilyTreeViewModel());
        }

        // إنشاء شجرة عائلية جديدة - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFamilyTree(CreateFamilyTreeViewModel viewModel)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("بدء عملية إنشاء شجرة جديدة للمستخدم: {UserId}", userId);

            if (ModelState.IsValid)
            {
                try
                {
                    // تحويل ViewModel إلى Model
                    var familyTree = new FamilyTree
                    {
                        Name = viewModel.Name,
                        Description = viewModel.Description ?? string.Empty,
                        Color = viewModel.Color,
                        UserId = userId.Value,
                        CreatedDate = DateTime.Now
                    };

                    _logger.LogInformation("إضافة الشجرة: {Name} للمستخدم: {UserId}", familyTree.Name, userId);

                    _context.FamilyTrees.Add(familyTree);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("تم حفظ الشجرة بنجاح، الرقم: {Id}", familyTree.Id);

                    TempData["SuccessMessage"] = $"تم إنشاء الشجرة العائلية '{familyTree.Name}' بنجاح!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ أثناء حفظ الشجرة العائلية");
                    ModelState.AddModelError("", $"حدث خطأ أثناء حفظ الشجرة: {ex.Message}");
                    TempData["ErrorMessage"] = $"حدث خطأ: {ex.Message}";
                }
            }
            else
            {
                _logger.LogWarning("النموذج غير صالح. أخطاء: {Errors}",
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                TempData["ErrorMessage"] = "البيانات غير صالحة. يرجى تصحيح الأخطاء أدناه.";
            }

            return View(viewModel);
        }

        // عرض الـ Logs مباشرة في المتصفح
        public IActionResult ViewLogs()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // الحصول على آخر 50 سطر من الـ Logs
                var logEntries = new List<string>();

                // إضافة بعض المعلومات عن الحالة الحالية
                logEntries.Add($"=== سجلات النظام - {DateTime.Now} ===");
                logEntries.Add($"المستخدم: {userId}");

                var trees = _context.FamilyTrees.Where(ft => ft.UserId == userId).ToList();
                logEntries.Add($"عدد الشجرات: {trees.Count}");

                var persons = _context.Persons.Where(p => p.FamilyTree.UserId == userId).ToList();
                logEntries.Add($"عدد الأفراد: {persons.Count}");

                // إضافة معلومات عن كل شجرة
                foreach (var tree in trees)
                {
                    var personCount = _context.Persons.Count(p => p.FamilyTreeId == tree.Id);
                    logEntries.Add($"الشجرة '{tree.Name}' (ID: {tree.Id}): {personCount} فرد");
                }

                ViewBag.Logs = logEntries;
                return View();
            }
            catch (Exception ex)
            {
                var errorLogs = new List<string>
                {
                    "خطأ في تحميل السجلات:",
                    ex.Message,
                    ex.StackTrace ?? "لا يوجد Stack Trace"
                };
                ViewBag.Logs = errorLogs;
                return View();
            }
        }

        // فحص قاعدة البيانات مباشرة
        public async Task<IActionResult> CheckDatabase()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var trees = await _context.FamilyTrees
                    .Where(ft => ft.UserId == userId)
                    .ToListAsync();

                var persons = await _context.Persons
                    .Include(p => p.FamilyTree)
                    .Where(p => p.FamilyTree.UserId == userId)
                    .ToListAsync();

                ViewBag.Trees = trees;
                ViewBag.Persons = persons;
                ViewBag.UserId = userId;

                _logger.LogInformation($"🔍 فحص قاعدة البيانات: {trees.Count} شجرة، {persons.Count} فرد");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ في فحص قاعدة البيانات");
                TempData["ErrorMessage"] = $"خطأ في فحص البيانات: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // API للحصول على أشخاص شجرة معينة
        public async Task<JsonResult> GetTreePersons(int treeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { });
            }

            // التحقق من أن الشجرة تتبع للمستخدم الحالي
            var treeExists = await _context.FamilyTrees
                .AnyAsync(ft => ft.Id == treeId && ft.UserId == userId);

            if (!treeExists)
            {
                return Json(new { });
            }

            var persons = await _context.Persons
                .Where(p => p.FamilyTreeId == treeId)
                .Select(p => new
                {
                    id = p.Id,
                    fullName = GetFullName(p) // استخدام الدالة المساعدة
                })
                .ToListAsync();

            return Json(persons);
        }

        // عرض الشجرة الرئيسية مع جميع الشجرات المرتبطة
        public async Task<IActionResult> MasterTree()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var mainTrees = await _context.FamilyTrees
                .Include(ft => ft.ChildTrees)
                .ThenInclude(child => child.Persons)
                .Include(ft => ft.Persons)
                .Where(ft => ft.UserId == userId && ft.ParentTreeId == null)
                .ToListAsync();

            return View(mainTrees);
        }

        // فحص قاعدة البيانات مباشرة
        public async Task<IActionResult> CheckDatabaseDirectly()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // الحصول على جميع الشجرات للمستخدم
                var trees = await _context.FamilyTrees
                    .Where(ft => ft.UserId == userId)
                    .ToListAsync();

                // الحصول على جميع الأشخاص للمستخدم
                var persons = await _context.Persons
                    .Include(p => p.FamilyTree)
                    .Where(p => p.FamilyTree.UserId == userId)
                    .ToListAsync();

                ViewBag.Trees = trees;
                ViewBag.Persons = persons;
                ViewBag.UserId = userId;

                _logger.LogInformation($"🔍 فحص قاعدة البيانات مباشرة:");
                _logger.LogInformation($"   - عدد الشجرات: {trees.Count}");
                _logger.LogInformation($"   - عدد الأفراد: {persons.Count}");

                foreach (var tree in trees)
                {
                    _logger.LogInformation($"   - الشجرة: {tree.Id} - {tree.Name}");
                }

                foreach (var person in persons)
                {
                    _logger.LogInformation($"   - الفرد: {person.Id} - {GetFullName(person)} - الشجرة: {person.FamilyTreeId}");
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ في فحص قاعدة البيانات مباشرة");
                TempData["ErrorMessage"] = $"خطأ في فحص البيانات: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // فحص جميع البيانات
        public async Task<IActionResult> DebugPersons(int familyTreeId = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var persons = await _context.Persons
                    .Include(p => p.FamilyTree)
                    .Where(p => p.FamilyTreeId == familyTreeId && p.FamilyTree.UserId == userId)
                    .ToListAsync();

                ViewBag.Persons = persons;
                ViewBag.FamilyTreeId = familyTreeId;

                _logger.LogInformation($"فحص البيانات: {persons.Count} فرد في الشجرة {familyTreeId}");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في فحص البيانات");
                TempData["ErrorMessage"] = $"خطأ في فحص البيانات: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // أكشن لحذف جميع أفراد الشجرة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllPersons(int familyTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var persons = await _context.Persons
                    .Where(p => p.FamilyTreeId == familyTreeId && p.FamilyTree.UserId == userId)
                    .ToListAsync();

                _context.Persons.RemoveRange(persons);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"تم حذف {persons.Count} فرد من الشجرة";
                return RedirectToAction("DeleteTree", new { id = familyTreeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في حذف أفراد الشجرة {familyTreeId}");
                TempData["ErrorMessage"] = "حدث خطأ في حذف الأفراد";
                return RedirectToAction("DeleteTree", new { id = familyTreeId });
            }
        }

        // فحص حالة شجرة معينة
        public async Task<IActionResult> CheckTreeStatus(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var tree = await _context.FamilyTrees
                    .Include(ft => ft.Persons)
                    .Include(ft => ft.ChildTrees)
                    .FirstOrDefaultAsync(ft => ft.Id == id && ft.UserId == userId);

                if (tree == null)
                {
                    TempData["ErrorMessage"] = "الشجرة غير موجودة";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Tree = tree;
                ViewBag.PersonCount = tree.Persons.Count;
                ViewBag.ChildTreeCount = tree.ChildTrees.Count;
                ViewBag.CanDelete = !tree.Persons.Any() && !tree.ChildTrees.Any();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"خطأ في فحص حالة الشجرة {id}");
                TempData["ErrorMessage"] = "حدث خطأ في فحص حالة الشجرة";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}