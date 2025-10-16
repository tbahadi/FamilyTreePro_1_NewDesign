using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FamilyTreePro.Models;

namespace FamilyTreePro.Controllers
{
    public class PersonController : Controller
    {
        private readonly AppDbContext _context;

        public PersonController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        // عرض أفراد شجرة عائلية محددة
        public async Task<IActionResult> Index(int familyTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // التحقق من أن الشجرة تتبع للمستخدم الحالي
            var familyTree = await _context.FamilyTrees
                .FirstOrDefaultAsync(ft => ft.Id == familyTreeId && ft.UserId == userId);

            if (familyTree == null)
            {
                return NotFound();
            }

            var persons = await _context.Persons
                .Include(p => p.Father)
                .Include(p => p.Mother)
                .Include(p => p.Occupation)
                .Include(p => p.Country)
                .Where(p => p.FamilyTreeId == familyTreeId)
                .OrderBy(p => p.FirstName)
                .ToListAsync();

            ViewBag.FamilyTreeId = familyTreeId;
            ViewBag.FamilyTreeName = familyTree.Name;
            return View(persons);
        }

        // إضافة فرد جديد
        public async Task<IActionResult> Create(int familyTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.FamilyTreeId = familyTreeId;
            ViewBag.Occupations = await _context.Occupations.ToListAsync();
            ViewBag.Countries = await _context.Countries.ToListAsync();

            // الحصول على الأشخاص من هذه الشجرة فقط لتحديد العلاقات
            ViewBag.PotentialFathers = await _context.Persons
                .Where(p => p.FamilyTreeId == familyTreeId && p.Gender == "Male")
                .ToListAsync();
            ViewBag.PotentialMothers = await _context.Persons
                .Where(p => p.FamilyTreeId == familyTreeId && p.Gender == "Female")
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Person person, int? fatherId, int? motherId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                person.FatherId = fatherId;
                person.MotherId = motherId;

                _context.Persons.Add(person);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { familyTreeId = person.FamilyTreeId });
            }

            ViewBag.FamilyTreeId = person.FamilyTreeId;
            ViewBag.Occupations = await _context.Occupations.ToListAsync();
            ViewBag.Countries = await _context.Countries.ToListAsync();
            ViewBag.PotentialFathers = await _context.Persons
                .Where(p => p.FamilyTreeId == person.FamilyTreeId && p.Gender == "Male")
                .ToListAsync();
            ViewBag.PotentialMothers = await _context.Persons
                .Where(p => p.FamilyTreeId == person.FamilyTreeId && p.Gender == "Female")
                .ToListAsync();

            return View(person);
        }

        // نقل شخص من شجرة إلى أخرى
        public async Task<IActionResult> MoveToTree(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var person = await _context.Persons
                .Include(p => p.FamilyTree)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (person == null)
            {
                return NotFound();
            }

            var otherTrees = await _context.FamilyTrees
                .Where(ft => ft.UserId == userId && ft.Id != person.FamilyTreeId)
                .ToListAsync();

            ViewBag.Person = person;
            ViewBag.OtherTrees = otherTrees;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> MoveToTree(int personId, int newTreeId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var person = await _context.Persons
                .Include(p => p.Children)
                .FirstOrDefaultAsync(p => p.Id == personId);

            if (person == null)
            {
                return NotFound();
            }

            // نقل الشخص إلى الشجرة الجديدة
            person.FamilyTreeId = newTreeId;

            // إذا كان لديه أبناء، نقلهم أيضاً
            foreach (var child in person.Children.ToList())
            {
                child.FamilyTreeId = newTreeId;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "تم نقل الشخص وأبنائه إلى الشجرة الجديدة!";

            return RedirectToAction("Index", new { familyTreeId = newTreeId });
        }
    }
}