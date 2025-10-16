using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FamilyTreePro.Models;

namespace FamilyTreePro.Controllers
{
    public class ManagementController : Controller
    {
        private readonly AppDbContext _context;

        public ManagementController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        // إدارة المهن
        public async Task<IActionResult> Occupations()
        {
            var occupations = await _context.Occupations.ToListAsync();
            return View(occupations);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOccupation(Occupation occupation)
        {
            if (ModelState.IsValid)
            {
                _context.Occupations.Add(occupation);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Occupations");
        }

        // إدارة الدول
        public async Task<IActionResult> Countries()
        {
            var countries = await _context.Countries.ToListAsync();
            return View(countries);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCountry(Country country)
        {
            if (ModelState.IsValid)
            {
                _context.Countries.Add(country);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Countries");
        }
    }
}