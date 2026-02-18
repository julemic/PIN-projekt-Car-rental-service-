using CarRentalService.Data;
using CarRentalService.Models;
using CarRentalService.Services;
using CarRentalService.Services.Pdf;
using Microsoft.AspNetCore.Authentication;
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

        
        // CARS (POST SEARCH)
        
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

        // PROFILE
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
                return View(model);

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.DateOfBirth = model.DateOfBirth;
            user.ResidenceAddress = model.ResidenceAddress;
            user.City = model.City;
            user.Nationality = model.Nationality;
            user.Oib = model.Oib;
            user.DriverLicenseNumber = model.DriverLicenseNumber;
            user.IdCardNumber = model.IdCardNumber;

            user.IsVerified = true;
            user.VerifiedAt = DateTime.UtcNow;

            await _userManager.UpdateAsync(user);

            TempData["Msg"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Profile));
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
                return RedirectToAction(nameof(Profile));
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

            TempData["Msg"] = $"Deposit of {pricing.Deposit} € successfully paid.";
            return RedirectToAction(nameof(MyRentals));
        }

        
        // RETURN CAR (final payment)
        
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

            TempData["Msg"] = $"Remaining amount of {remainingAmount} € successfully paid.";
            return RedirectToAction(nameof(MyRentals));
        }

        
        // ACCIDENT REPORT (GET)
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CreateAccidentReport(int rentalId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // rentals korisnika za dropdown (zadnjih 50)
            var rentals = await _db.Rentals
                .Include(r => r.Vehicle)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(50)
                .ToListAsync();

            ViewBag.UserRentals = rentals;

            // automatski popuni vozača iz ApplicationUser
            var model = new AccidentReport
            {
                FullName = $"{user.FirstName} {user.LastName}",
                Oib = user.Oib,
                DriverLicenseNumber = user.DriverLicenseNumber,
                Phone = user.PhoneNumber
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccidentReport(AccidentReport model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // UserId se postavlja server-side (ne iz forme)
            ModelState.Remove(nameof(AccidentReport.UserId));
            ModelState.Remove(nameof(AccidentReport.User));

            // Ponovno napuni rentals za slučaj da validacija faila i vraća view
            var rentals = await _db.Rentals
                .Include(r => r.Vehicle)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(50)
                .ToListAsync();

            ViewBag.UserRentals = rentals;

            // sigurnosno: Rental mora biti od tog usera
            var rentalExists = await _db.Rentals.AnyAsync(r => r.Id == model.RentalId && r.UserId == user.Id);
            if (!rentalExists)
                ModelState.AddModelError(nameof(AccidentReport.RentalId), "Please select a valid rental.");

            if (!ModelState.IsValid)
                return View(model);

            model.UserId = user.Id;
            model.CreatedAt = DateTime.UtcNow;

            _db.AccidentReports.Add(model);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Accident report submitted successfully.";
            return RedirectToAction(nameof(CreateAccidentReport));
        }
        //download pdf 
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccidentReportPdf(AccidentReport model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ModelState.Remove("UserId");

            if (!ModelState.IsValid)
                return View("CreateAccidentReport", model);

            model.UserId = user.Id;
            model.CreatedAt = DateTime.UtcNow;

            _db.AccidentReports.Add(model);
            await _db.SaveChangesAsync();

            var pdfBytes = _pdfGenerator.Generate(model);

            return File(pdfBytes,
                "application/pdf",
                $"AccidentReport_{DateTime.Now:yyyyMMddHHmm}.pdf");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminAccidentReports()
        {
            var reports = await _db.AccidentReports
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reports);
        }

        // ADMIN DELETE ACCIDENT REPORT
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccidentReport(int id)
        {
            var report = await _db.AccidentReports.FindAsync(id);

            if (report == null)
                return NotFound();

            _db.AccidentReports.Remove(report);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(AdminAccidentReports));
        }


        
        // RENTAL VIEW BUILDER
        
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

        // LOGOUT
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return RedirectToAction(nameof(Index));
        }

    }
}
