using CarRentalService.Data;
using CarRentalService.Models;
using CarRentalService.Services;
using CarRentalService.Services.Pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace CarRentalService.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRentalPricingService _pricingService;
        private readonly AccidentReportPdfGenerator _pdfGenerator;

        public HomeController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IRentalPricingService pricingService,
            AccidentReportPdfGenerator pdfGenerator)
        {
            _db = db;
            _userManager = userManager;
            _pricingService = pricingService;
            _pdfGenerator = pdfGenerator;
        }

        public IActionResult Index() => View();
        public IActionResult Privacy() => View();

        
        // CARS (GET)
        

        [HttpGet]
        public async Task<IActionResult> Cars()
        {
            await FillCarsViewBags("All offers", "");
            ViewBag.ShowVehicles = false;

            return View(new CarsSearchVm
            {
                SelectedInsurancePlan = InsurancePlan.Basic
            });
        }

        
        // CARS 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cars(CarsSearchVm vm, string Category = "All offers", string Sort = "")
        {
            await FillCarsViewBags(Category, Sort);

            ViewBag.ShowVehicles = false;
            ViewBag.Vehicles = new List<Vehicle>();
            ViewBag.AvailableMap = new Dictionary<int, int>();

            if (!ModelState.IsValid)
                return View(vm);

            if (vm.Pickup == null || vm.Return == null)
            {
                ModelState.AddModelError("", "Pickup and return date are required.");
                return View(vm);
            }

            var pickup = vm.Pickup.Value.Date;
            var ret = vm.Return.Value.Date;

            if (pickup < DateTime.Today)
            {
                ModelState.AddModelError("", "Pickup date cannot be in the past.");
                return View(vm);
            }

            if (ret <= pickup)
            {
                ModelState.AddModelError("", "Return date must be after pickup date.");
                return View(vm);
            }

            var query = _db.Vehicles.Where(v => v.IsActive);

            if (!string.IsNullOrWhiteSpace(Category) && Category != "All offers")
                query = query.Where(v => v.Category == Category);

            query = Sort switch
            {
                "price_asc" => query.OrderBy(v => v.DailyPrice),
                "price_desc" => query.OrderByDescending(v => v.DailyPrice),
                _ => query.OrderBy(v => v.Id)
            };

            var vehicles = await query.ToListAsync();

            var overlapping = await _db.Rentals
                .Where(r =>
                    r.Status == RentalStatus.Reserved &&
                    r.Pickup < ret &&
                    pickup < r.Return)
                .GroupBy(r => r.VehicleId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

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

        
        // RENT 

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rent(CarsSearchVm vm)
        {
            if (!ModelState.IsValid ||
                vm.VehicleId == null ||
                vm.Pickup == null ||
                vm.Return == null)
            {
                TempData["Msg"] = "Invalid rental data.";
                return RedirectToAction(nameof(Cars));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!user.IsVerified)
            {
                TempData["Msg"] = "You must verify your identity before renting.";
                return RedirectToAction("CompleteProfile");
            }

            var vehicle = await _db.Vehicles
                .FirstOrDefaultAsync(v => v.Id == vm.VehicleId && v.IsActive);

            if (vehicle == null) return NotFound();

            var pricing = _pricingService.CalculatePricing(
                vehicle,
                vm.Pickup.Value.Date,
                vm.Return.Value.Date,
                vm.SelectedInsurancePlan);

            var rental = new Rental
            {
                VehicleId = vehicle.Id,
                Pickup = vm.Pickup.Value.Date,
                Return = vm.Return.Value.Date,
                UserId = user.Id,
                InsurancePlan = vm.SelectedInsurancePlan,
                Status = RentalStatus.Reserved,
                CreatedAt = DateTime.UtcNow,

                
                DepositPaid = pricing.Deposit,
                IsDepositPaid = true,
                DepositPaidAt = DateTime.UtcNow
            };

            _db.Rentals.Add(rental);
            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Reservation created. Deposit of {pricing.Deposit} € successfully paid.";
            return RedirectToAction(nameof(MyRentals));
        }

        
        // RETURN CAR 

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnCar(int rentalId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var rental = await _db.Rentals
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r => r.Id == rentalId && r.UserId == userId);

            if (rental == null) return NotFound();

            var pricing = _pricingService.CalculatePricing(
                rental.Vehicle!,
                rental.Pickup,
                rental.Return,
                rental.InsurancePlan);

            var remainingAmount = pricing.TotalPrice - rental.DepositPaid;

            rental.FinalAmountPaid = remainingAmount;
            rental.IsFullyPaid = true;
            rental.FullyPaidAt = DateTime.UtcNow;

            rental.Status = RentalStatus.Returned;
            rental.ReturnedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Msg"] = $"Vehicle returned. Remaining amount of {remainingAmount} € successfully paid.";
            return RedirectToAction(nameof(MyRentals));
        }

        
        // CANCEL RESERVATION 

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(int rentalId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var rental = await _db.Rentals
                .Include(r => r.Vehicle)
                .FirstOrDefaultAsync(r =>
                    r.Id == rentalId &&
                    r.UserId == userId &&
                    r.Status == RentalStatus.Reserved);

            if (rental == null) return NotFound();

            var pricing = _pricingService.CalculatePricing(
                rental.Vehicle!,
                rental.Pickup,
                rental.Return,
                rental.InsurancePlan);

            rental.Status = RentalStatus.Cancelled;

            if (pricing.CanFreeCancel)
            {
                rental.DepositPaid = 0;
                rental.IsDepositPaid = false;
                TempData["Msg"] = "Reservation cancelled. Deposit refunded.";
            }
            else
            {
                TempData["Msg"] = "Reservation cancelled. Deposit retained.";
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(MyRentals));
        }

        
        // RENTAL VIEWS
        

        [Authorize]
        public async Task<IActionResult> MyRentals()
        {
            return await BuildRentalView(nameof(MyRentals),
                r => r.Status == RentalStatus.Reserved);
        }

        [Authorize]
        public async Task<IActionResult> RentalHistory()
        {
            return await BuildRentalView(nameof(RentalHistory),
                r => r.Status == RentalStatus.Returned ||
                     r.Status == RentalStatus.Cancelled);
        }

        
        // SHARED BUILDER
        

        private async Task<IActionResult> BuildRentalView(
            string viewName,
            Expression<Func<Rental, bool>> filter)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var rentals = await _db.Rentals
                .Include(r => r.Vehicle)
                .Where(r => r.UserId == userId)
                .Where(filter)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var viewModel = rentals.Select(r =>
            {
                var pricing = _pricingService.CalculatePricing(
                    r.Vehicle!,
                    r.Pickup,
                    r.Return,
                    r.InsurancePlan);

                return new RentalViewModel
                {
                    Id = r.Id,
                    Brand = r.Vehicle!.Brand,
                    Model = r.Vehicle.Model,
                    Category = r.Vehicle.Category,
                    Status = r.Status,
                    Pickup = r.Pickup,
                    Return = r.Return,
                    TotalPrice = pricing.TotalPrice,
                    DepositAmount = pricing.Deposit,
                    FreeCancellationUntil = pricing.FreeCancellationUntil,
                    CanFreeCancel = pricing.CanFreeCancel,
                    ReturnedAt = r.ReturnedAt
                };
            }).ToList();

            return View(viewName, viewModel);
        }

        
        // HELPERS
        

        private async Task FillCarsViewBags(string category, string sort)
        {
            ViewBag.Categories = await _db.Vehicles
                .Where(v => v.IsActive)
                .Select(v => v.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.SelectedCategory = category;
            ViewBag.Sort = sort;
        }
    }
}
