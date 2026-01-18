using CarRentalService.Data;
using CarRentalService.Models;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalService.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index() => View();

        public IActionResult Privacy() => View();

        [HttpGet]
        public IActionResult Cars()
        {
            ViewBag.Categories = _db.Vehicles
                .Select(v => v.Category)
                .Where(c => c != null && c != "")
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            ViewBag.SelectedCategory = "All offers";

            return View(new CarsSearchVm());
        }

        [HttpPost]
        public IActionResult Cars(CarsSearchVm vm, string category = "All offers", string Sort = "")

        {
            // uvijek napuni kategorije da se tipke prikazuju
            ViewBag.Categories = _db.Vehicles
            .Select(v => v.Category)
            .Where(c => c != null && c != "")
            .Distinct()
            .OrderBy(c => c)
            .ToList();

            ViewBag.SelectedCategory = category;

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var query = _db.Vehicles.AsQueryable();

            if (category != "All offers")
            {
                query = query.Where(v => v.Category == category);
            }

            // SORTIRANJE
            switch (Sort)
            {
                case "price_asc":
                    query = query.OrderBy(v => v.DailyPrice);
                    break;

                case "price_desc":
                    query = query.OrderByDescending(v => v.DailyPrice);
                    break;
            }

            ViewBag.ShowVehicles = true;
            ViewBag.Vehicles = query.ToList();
            ViewBag.Sort = Sort;


            return View(vm);
        }

    }
}

