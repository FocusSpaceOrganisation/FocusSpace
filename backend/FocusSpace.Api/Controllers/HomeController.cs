using Microsoft.AspNetCore.Mvc;
using FocusSpace.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FocusSpace.Api.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var planets = await _context.Planets
                .AsNoTracking()
                .OrderBy(p => p.OrderNumber)
                .ToListAsync();

            return View(planets);
        }
    }
}