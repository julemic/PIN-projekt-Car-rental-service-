using CarRentalService.Data;
using CarRentalService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalService.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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
            ViewBag.Sort = "";

            return View(new CarsSearchVm());
        }

        
        [HttpPost]
        public IActionResult Cars(CarsSearchVm vm, string Category = "All offers", string Sort = "")
        {
            // uvijek napuni kategorije da tipke postoje
            ViewBag.Categories = _db.Vehicles
                .Select(v => v.Category)
                .Where(c => c != null && c != "")
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            ViewBag.SelectedCategory = Category;
            ViewBag.Sort = Sort;

            if (!ModelState.IsValid)
                return View(vm);

            // validacija datuma (Return mora biti nakon Pickup)
            if (vm.Pickup == null || vm.Return == null || vm.Return <= vm.Pickup)
            {
                ModelState.AddModelError("", "Return time must be after pickup time.");
                return View(vm);
            }

            var pickup = vm.Pickup.Value;
            var ret = vm.Return.Value;

            // osnovni upit za vozila
            var query = _db.Vehicles.Where(v => v.IsActive);


            // filter kategorije
            if (!string.IsNullOrWhiteSpace(Category) && Category != "All offers")
                query = query.Where(v => v.Category == Category);

            // sortiranje
            query = Sort switch
            {
                "price_asc" => query.OrderBy(v => v.DailyPrice),
                "price_desc" => query.OrderByDescending(v => v.DailyPrice),
                _ => query
            };

            var vehicles = query.ToList();

            // izračun dostupnosti za ODABRANI termin (TotalQuantity - overlapping rentals)
            var overlapping = _db.Rentals
                .Where(r => !r.IsReturned
                            && r.Pickup < ret
                            && pickup < r.Return)
                .GroupBy(r => r.VehicleId)
                .Select(g => new { VehicleId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.VehicleId, x => x.Count);

            var availableMap = new Dictionary<int, int>();
            foreach (var v in vehicles)
            {
                var used = overlapping.TryGetValue(v.Id, out var cnt) ? cnt : 0;
                var available = v.TotalQuantity - used;
                if (available < 0) available = 0;
                availableMap[v.Id] = available;
            }

            ViewBag.AvailableMap = availableMap;
            ViewBag.ShowVehicles = true;
            ViewBag.Vehicles = vehicles;

            return View(vm);
        }

        //  RENT NOW 
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rent(CarsSearchVm vm, string Category = "All offers", string Sort = "")
        {
            if (vm.VehicleId == null || vm.Pickup == null || vm.Return == null || vm.Return <= vm.Pickup)
                return RedirectToAction("Cars");

            var pickup = vm.Pickup.Value;
            var ret = vm.Return.Value;
            var vehicleId = vm.VehicleId.Value;

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
            if (vehicle == null) return NotFound();

            // provjeri koliko je već zauzeto u tom terminu
            var usedCount = await _db.Rentals.CountAsync(r =>
                !r.IsReturned &&
                r.VehicleId == vehicleId &&
                r.Pickup < ret &&
                pickup < r.Return);

            var available = vehicle.TotalQuantity - usedCount;
            if (available <= 0)
            {
                TempData["Msg"] = "This vehicle is not available for the selected time.";
                return RedirectToAction("Cars");
            }

            // kreiraj rental
            _db.Rentals.Add(new Rental
            {
                VehicleId = vehicleId,
                Pickup = pickup,
                Return = ret,
                UserId = userId,
                IsReturned = false
            });

            await _db.SaveChangesAsync();

            TempData["Msg"] = "Vehicle rented successfully!";
            return RedirectToAction("MyRentals");
        }

        //  rentals korisnika
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyRentals()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var rentals = await _db.Rentals
                .Include(r => r.Vehicle)
                .Where(r => r.UserId == userId && !r.IsReturned)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(rentals);
        }

        //  vrati auto
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnCar(int rentalId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var rental = await _db.Rentals.FirstOrDefaultAsync(r => r.Id == rentalId && r.UserId == userId);
            if (rental == null) return NotFound();

            rental.IsReturned = true;
            rental.ReturnedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Msg"] = "Vehicle returned.";
            return RedirectToAction("MyRentals");
        }
    }
}
