using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FamilyTreePro.Models;
using Microsoft.Extensions.Logging;

namespace FamilyTreePro.Controllers
{
    public class CombinedTreeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CombinedTreeController> _logger;

        public CombinedTreeController(AppDbContext context, ILogger<CombinedTreeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        // عرض الشجرات المدمجة
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var combinedTrees = await _context.CombinedTrees
                .Include(ct => ct.CombinedTrees)
                .ThenInclude(cti => cti.FamilyTree)
                .Where(ct => ct.UserId == userId)
                .ToListAsync();

            return View(combinedTrees);
        }

        // إنشاء شجرة مدمجة جديدة
        [HttpGet]
        public IActionResult Create()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            // جلب الشجرات المتاحة للمستخدم
            var availableTrees = _context.FamilyTrees
                .Where(ft => ft.UserId == userId)
                .ToList();

            ViewBag.AvailableTrees = availableTrees;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CombinedTree combinedTree, List<int> selectedTrees, List<int?> connectionPersonIds)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid && selectedTrees != null && selectedTrees.Any())
            {
                try
                {
                    combinedTree.UserId = userId.Value;
                    _context.CombinedTrees.Add(combinedTree);
                    await _context.SaveChangesAsync();

                    // إضافة الشجرات المختارة
                    for (int i = 0; i < selectedTrees.Count; i++)
                    {
                        var treeItem = new CombinedTreeItem
                        {
                            CombinedTreeId = combinedTree.Id,
                            FamilyTreeId = selectedTrees[i],
                            ConnectionPersonId = connectionPersonIds[i]
                        };
                        _context.CombinedTreeItems.Add(treeItem);
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تم إنشاء الشجرة المدمجة بنجاح!";
                    return RedirectToAction("ViewTree", new { id = combinedTree.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في إنشاء الشجرة المدمجة");
                    TempData["ErrorMessage"] = "حدث خطأ أثناء الإنشاء";
                }
            }

            ViewBag.AvailableTrees = _context.FamilyTrees.Where(ft => ft.UserId == userId).ToList();
            return View(combinedTree);
        }

        // عرض الشجرة المدمجة
        public async Task<IActionResult> ViewTree(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Account");

            var combinedTree = await _context.CombinedTrees
                .Include(ct => ct.CombinedTrees)
                .ThenInclude(cti => cti.FamilyTree)
                .ThenInclude(ft => ft.Persons)
                .Include(ct => ct.CombinedTrees)
                .ThenInclude(cti => cti.ConnectionPerson)
                .FirstOrDefaultAsync(ct => ct.Id == id && ct.UserId == userId);

            if (combinedTree == null)
            {
                TempData["ErrorMessage"] = "الشجرة المدمجة غير موجودة";
                return RedirectToAction("Index");
            }

            return View(combinedTree);
        }

        // API لجلب أشخاص شجرة معينة (للاكمال التلقائي)
        public async Task<JsonResult> GetTreePersons(int treeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { });

            var persons = await _context.Persons
                .Where(p => p.FamilyTreeId == treeId && p.FamilyTree.UserId == userId)
                .Select(p => new { id = p.Id, name = p.FullName })
                .ToListAsync();

            return Json(persons);
        }
    }
}