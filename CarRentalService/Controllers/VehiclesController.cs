using CarRentalService.Data;
using CarRentalService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRentalService.Constants;

namespace CarRentalService.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class VehiclesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public VehiclesController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }


        public async Task<IActionResult> Index()
        {
            var vehicles = await _db.Vehicles
                .OrderByDescending(v => v.Id)
                .ToListAsync();

            return View(vehicles);
        }


        [HttpGet]
        public IActionResult Create()
        {
            return View(new Vehicle());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vehicle vehicle, IFormFile? imageFile)
        {
            if (imageFile != null)
            {
                var path = await SaveImage(imageFile);
                if (path == null)
                {
                    ModelState.AddModelError("", "Invalid image. Allowed: .jpg, .jpeg, .png, .webp (max 5MB).");
                    return View(vehicle);
                }
                vehicle.ImageUrl = path;
            }

            if (!ModelState.IsValid) return View(vehicle);

            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vehicle = await _db.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            return View(vehicle);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Vehicle vehicle, IFormFile? imageFile)
        {
            if (id != vehicle.Id) return BadRequest();

            if (imageFile != null)
            {
                var path = await SaveImage(imageFile);
                if (path == null)
                {
                    ModelState.AddModelError("", "Invalid image. Allowed: .jpg, .jpeg, .png, .webp (max 5MB).");
                    return View(vehicle);
                }
                var existing = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
                DeleteImage(existing?.ImageUrl);
                vehicle.ImageUrl = path;
            }

            if (!ModelState.IsValid) return View(vehicle);

            _db.Entry(vehicle).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var vehicle = await _db.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            return View(vehicle);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Index));

            var vehicle = await _db.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            var hasRentals = await _db.Rentals.AnyAsync(r => r.VehicleId == id);
            if (hasRentals)
            {
                vehicle.IsActive = false;
                await _db.SaveChangesAsync();
                TempData[TempDataKeys.Message] = "Vehicle has rentals, so it was deactivated instead of deleted.";
                return RedirectToAction(nameof(Index));
            }

            DeleteImage(vehicle.ImageUrl);
            _db.Vehicles.Remove(vehicle);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private const long MaxFileSize = 5 * 1024 * 1024;

        private async Task<string?> SaveImage(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext))
                return null;

            if (file.Length > MaxFileSize)
                return null;

            var uploads = Path.Combine(_env.WebRootPath, Upload.VehicleImagePath);

            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid() + ext;
            var fullPath = Path.Combine(uploads, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return Upload.VehicleImageUrlPrefix + fileName;
        }

        private void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !imagePath.StartsWith(Upload.VehicleImageUrlPrefix))
                return;

            var fullPath = Path.Combine(_env.WebRootPath, imagePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}
