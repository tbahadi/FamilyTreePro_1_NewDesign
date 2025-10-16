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
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var familyTrees = await _context.FamilyTrees
                .Include(ft => ft.ChildTrees)
                .Include(ft => ft.ConnectionPerson)
                .Where(ft => ft.UserId == userId)
                .OrderByDescending(ft => ft.CreatedDate)
                .ToListAsync();

            return View(familyTrees);
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

                    // تحديث الشخص ليكون نقطة ربط
                    if (connectionPersonId.HasValue)
                    {
                        var person = await _context.Persons.FindAsync(connectionPersonId);
                        if (person != null)
                        {
                            person.IsConnectionPoint = true;
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "تم ربط الشجرات بنجاح!";
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
                .Select(p => new {
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

        // حذف شجرة عائلية
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

            if (tree.Persons.Any() || tree.ChildTrees.Any())
            {
                TempData["ErrorMessage"] = "لا يمكن حذف الشجرة لأنها تحتوي على أفراد أو شجرات فرعية";
                return RedirectToAction(nameof(Index));
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
                return RedirectToAction("Login", "Account");
            }

            var tree = await _context.FamilyTrees
                .FirstOrDefaultAsync(ft => ft.Id == id && ft.UserId == userId);

            if (tree != null)
            {
                try
                {
                    _context.FamilyTrees.Remove(tree);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "تم حذف الشجرة بنجاح!";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ أثناء حذف الشجرة");
                    TempData["ErrorMessage"] = $"حدث خطأ أثناء الحذف: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}