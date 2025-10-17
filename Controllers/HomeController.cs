using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FamilyTreePro.Models;


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

        // الصفحة الرئيسية - عرض جميع الشجرات للمستخدم
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

        // ربط شجرة بأخرى - GET
        public async Task<IActionResult> ConnectTrees(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentTree = await _context.FamilyTrees
                .FirstOrDefaultAsync(ft => ft.Id == id && ft.UserId == userId);

            if (currentTree == null)
            {
                TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
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
        // ربط شجرة بأخرى - POST (بدون IsConnectionPoint)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConnectTrees(int treeId, int? parentTreeId, int? connectionPersonId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

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
                    tree.ParentTreeId = parentTreeId;
                    tree.ConnectionPersonId = connectionPersonId;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "تم ربط الشجرات بنجاح!";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ أثناء ربط الشجرات");
                    TempData["ErrorMessage"] = $"حدث خطأ أثناء الربط: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
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
                    fullName = p.FullName
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
                    _logger.LogInformation($"   - الفرد: {person.Id} - {person.FullName} - الشجرة: {person.FamilyTreeId}");
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

        // حذف شجرة عائلية
        // عرض تأكيد حذف شجرة - GET
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

            // التحقق من إمكانية الحذف
            if (tree.Persons.Any() || tree.ChildTrees.Any())
            {
                ViewBag.CanDelete = false;
                ViewBag.PersonCount = tree.Persons.Count;
                ViewBag.ChildTreeCount = tree.ChildTrees.Count;
            }
            else
            {
                ViewBag.CanDelete = true;
            }

            return View(tree);
        }

        [HttpPost, ActionName("DeleteTree")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTreeConfirmed(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("❌ محاولة حذف شجرة بدون تسجيل دخول");
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation($"🔍 بدء محاولة حذف الشجرة {id} للمستخدم {userId}");

            try
            {
                var tree = await _context.FamilyTrees
                    .Include(ft => ft.Persons)
                    .Include(ft => ft.ChildTrees)
                    .FirstOrDefaultAsync(ft => ft.Id == id && ft.UserId == userId);

                if (tree == null)
                {
                    _logger.LogWarning($"❌ الشجرة {id} غير موجودة أو لا تتبع للمستخدم {userId}");
                    TempData["ErrorMessage"] = "الشجرة غير موجودة أو لا تملك صلاحية الوصول لها";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation($"📊 معلومات الشجرة المراد حذفها:");
                _logger.LogInformation($"   - الاسم: {tree.Name}");
                _logger.LogInformation($"   - عدد الأفراد: {tree.Persons?.Count ?? 0}");
                _logger.LogInformation($"   - عدد الشجرات الفرعية: {tree.ChildTrees?.Count ?? 0}");

                // التحقق من وجود أفراد أو شجرات فرعية
                if (tree.Persons?.Any() == true || tree.ChildTrees?.Any() == true)
                {
                    _logger.LogWarning($"❌ لا يمكن حذف الشجرة {id} لأنها تحتوي على:");
                    _logger.LogWarning($"   - أفراد: {tree.Persons?.Count ?? 0}");
                    _logger.LogWarning($"   - شجرات فرعية: {tree.ChildTrees?.Count ?? 0}");

                    TempData["ErrorMessage"] = "لا يمكن حذف الشجرة لأنها تحتوي على أفراد أو شجرات فرعية";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation($"🗑️ بدء حذف الشجرة {id}");

                // الحذف
                _context.FamilyTrees.Remove(tree);
                int recordsAffected = await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ تم حذف الشجرة بنجاح! السجلات المتأثرة: {recordsAffected}");

                TempData["SuccessMessage"] = "تم حذف الشجرة بنجاح!";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"❌ خطأ في قاعدة البيانات أثناء حذف الشجرة {id}");
                _logger.LogError($"تفاصيل الخطأ الداخلية: {dbEx.InnerException?.Message}");

                TempData["ErrorMessage"] = $"حدث خطأ في قاعدة البيانات أثناء الحذف: {dbEx.InnerException?.Message ?? dbEx.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ خطأ غير متوقع أثناء حذف الشجرة {id}");
                TempData["ErrorMessage"] = $"حدث خطأ غير متوقع أثناء الحذف: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
    }