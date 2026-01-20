using CarRentalService.Data;
using CarRentalService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalService.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VehiclesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public VehiclesController(ApplicationDbContext db)
        {
            _db = db;
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
        public async Task<IActionResult> Create(Vehicle vehicle)
        {
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
        public async Task<IActionResult> Edit(int id, Vehicle vehicle)
        {
            if (id != vehicle.Id) return BadRequest();

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
            var vehicle = await _db.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();

            
            var hasRentals = await _db.Rentals.AnyAsync(r => r.VehicleId == id);
            if (hasRentals)
            {
                
                vehicle.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Vehicle has rentals, so it was deactivated instead of deleted.";
                return RedirectToAction(nameof(Index));
            }

            _db.Vehicles.Remove(vehicle);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
