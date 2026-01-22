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
            FillCarsViewBags(category: "All offers", sort: "");

            return View(new CarsSearchVm
            {
                SelectedInsurancePlan = InsurancePlan.Basic
            });
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cars(CarsSearchVm vm, string Category = "All offers", string Sort = "")
        {
            FillCarsViewBags(Category, Sort);

            if (!ModelState.IsValid)
                return View(vm);

            if (vm.Pickup == null || vm.Return == null || vm.Return <= vm.Pickup)
            {
                ModelState.AddModelError("", "Return time must be after pickup time.");
                return View(vm);
            }

            var pickup = vm.Pickup.Value;
            var ret = vm.Return.Value;

            
            var query = _db.Vehicles.Where(v => v.IsActive);

            
            if (!string.IsNullOrWhiteSpace(Category) && Category != "All offers")
                query = query.Where(v => v.Category == Category);

            
            query = Sort switch
            {
                "price_asc" => query.OrderBy(v => v.DailyPrice),
                "price_desc" => query.OrderByDescending(v => v.DailyPrice),
                _ => query.OrderBy(v => v.Id)
            };

            var vehicles = query.ToList();

            
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

            ViewBag.ShowVehicles = true;
            ViewBag.Vehicles = vehicles;
            ViewBag.AvailableMap = availableMap;

            return View(vm);
        }

        
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rent(CarsSearchVm vm, string Category = "All offers", string Sort = "")
        {
            if (vm.VehicleId == null || vm.Pickup == null || vm.Return == null || vm.Return <= vm.Pickup)
                return RedirectToAction(nameof(Cars));

            var pickup = vm.Pickup.Value;
            var ret = vm.Return.Value;
            var vehicleId = vm.VehicleId.Value;

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId && v.IsActive);
            if (vehicle == null) return NotFound();

            
            var usedCount = await _db.Rentals.CountAsync(r =>
                !r.IsReturned &&
                r.VehicleId == vehicleId &&
                r.Pickup < ret &&
                pickup < r.Return);

            var available = vehicle.TotalQuantity - usedCount;
            if (available <= 0)
            {
                TempData["Msg"] = "This vehicle is not available for the selected time.";
                return RedirectToAction(nameof(Cars));
            }

            
            decimal insurancePerDay = vm.SelectedInsurancePlan switch
            {
                InsurancePlan.Basic => 0m,
                InsurancePlan.Medium => 17m,
                InsurancePlan.Total => 22m,
                _ => 0m
            };

            _db.Rentals.Add(new Rental
            {
                VehicleId = vehicleId,
                Pickup = pickup,
                Return = ret,
                UserId = userId,
                IsReturned = false,

                InsurancePlan = vm.SelectedInsurancePlan,
                
            });

            await _db.SaveChangesAsync();

            TempData["Msg"] = "Vehicle rented successfully!";
            return RedirectToAction(nameof(MyRentals));
        }

       
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
            return RedirectToAction(nameof(MyRentals));
        }

        
        private void FillCarsViewBags(string category, string sort)
        {
            var categories = _db.Vehicles
                .Where(v => v.IsActive)
                .Select(v => v.Category)
                .Where(c => c != null && c != "")
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = string.IsNullOrWhiteSpace(category) ? "All offers" : category;
            ViewBag.Sort = sort ?? "";
        }
    }
}
