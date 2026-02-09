using CarRentalService.Data;
using CarRentalService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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

        
        // CARS (GET)
        
        [HttpGet]
        public IActionResult Cars()
        {
            FillCarsViewBags("All offers", "");

            return View(new CarsSearchVm
            {
                SelectedInsurancePlan = InsurancePlan.Basic
            });
        }

        
        // CARS (POST – SEARCH)
        
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

            if (Category != "All offers")
                query = query.Where(v => v.Category == Category);

            query = Sort switch
            {
                "price_asc" => query.OrderBy(v => v.DailyPrice),
                "price_desc" => query.OrderByDescending(v => v.DailyPrice),
                _ => query.OrderBy(v => v.Id)
            };

            var vehicles = query.ToList();

            var overlapping = _db.Rentals
                .Where(r => !r.IsReturned && r.Pickup < ret && pickup < r.Return)
                .GroupBy(r => r.VehicleId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionary(x => x.Key, x => x.Count);

            var availableMap = new Dictionary<int, int>();
            foreach (var v in vehicles)
            {
                var used = overlapping.TryGetValue(v.Id, out var cnt) ? cnt : 0;
                availableMap[v.Id] = Math.Max(0, v.TotalQuantity - used);
            }

            ViewBag.ShowVehicles = true;
            ViewBag.Vehicles = vehicles;
            ViewBag.AvailableMap = availableMap;

            return View(vm);
        }

        
        // RENT VEHICLE
        
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rent(CarsSearchVm vm)
        {
            if (vm.VehicleId == null || vm.Pickup == null || vm.Return == null || vm.Return <= vm.Pickup)
                return RedirectToAction(nameof(Cars));

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vm.VehicleId && v.IsActive);
            if (vehicle == null) return NotFound();

            var used = await _db.Rentals.CountAsync(r =>
                !r.IsReturned &&
                r.VehicleId == vehicle.Id &&
                r.Pickup < vm.Return &&
                vm.Pickup < r.Return);

            if (vehicle.TotalQuantity - used <= 0)
            {
                TempData["Msg"] = "Vehicle not available for selected dates.";
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
                VehicleId = vehicle.Id,
                Pickup = vm.Pickup.Value,
                Return = vm.Return.Value,
                UserId = userId,
                InsurancePlan = vm.SelectedInsurancePlan,
                IsReturned = false
            });

            await _db.SaveChangesAsync();

            TempData["Msg"] = "Vehicle rented successfully!";
            return RedirectToAction(nameof(MyRentals));
        }

        
        // MY RENTALS
        
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

        
        // Vrati auto
       
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

        
        // Prijava štete pdf
        
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AccidentReportPdf(AccidentReportVm vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["Msg"] = "Please fill all required accident report fields.";
                return RedirectToAction(nameof(MyRentals));
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var rental = await _db.Rentals
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == vm.RentalId && r.UserId == userId);

            if (rental == null) return NotFound();

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);

                    page.Header().Text("ACCIDENT REPORT")
                        .FontSize(18).Bold().AlignCenter();

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Text($"Rental ID: {rental.Id}");
                        col.Item().Text($"Vehicle: {rental.Vehicle!.Brand} {rental.Vehicle.Model}");
                        col.Item().Text("Vehicle info").Bold();
                        col.Item().Text($"Vehicle ID: {vm.VehicleId}");
                        col.Item().Text($"Brand: {vm.VehicleBrand}");
                        col.Item().Text($"Model: {vm.VehicleModel}");
                        col.Item().Text($"Category: {vm.VehicleCategory}");

                        col.Item().LineHorizontal(1);

                        col.Item().Text($"Accident date: {vm.AccidentDate}");
                        col.Item().Text($"Location: {vm.Location}");
                        col.Item().Text($"Description:");
                        col.Item().Border(1).Padding(10).Text(vm.Description);
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken2));
                        text.Span("Car Rental Service • Accident report");
                    });
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"AccidentReport_{rental.Id}.pdf");
        }

        
        // HELPERS
        
        private void FillCarsViewBags(string category, string sort)
        {
            ViewBag.Categories = _db.Vehicles
                .Where(v => v.IsActive)
                .Select(v => v.Category)
                .Where(c => c != null && c != "")
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            ViewBag.SelectedCategory = category;
            ViewBag.Sort = sort;
        }
    }
}
