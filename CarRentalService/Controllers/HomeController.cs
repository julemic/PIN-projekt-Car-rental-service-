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

        //  Prikaz forme
        [HttpGet]
        public IActionResult Cars()
        {
            return View(new CarsSearchVm());
        }

        //  Kad korisnik pošalje datume
        [HttpPost]
        public IActionResult Cars(CarsSearchVm vm)
        {
            if (!ModelState.IsValid)
            {
                // ostani na formi i prikaži greške
                return View(vm);
            }

            // datumi su OK ->  dohvatimo vozila
            var vehicles = _db.Vehicles.ToList();

            ViewBag.ShowVehicles = true;
            ViewBag.Vehicles = vehicles;

            return View(vm);
        }
    }
}
