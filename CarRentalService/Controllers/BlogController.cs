using CarRentalService.Data;
using CarRentalService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalService.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public BlogController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
        }

        // LIST
        public async Task<IActionResult> Index()
        {
            var posts = await _db.BlogPosts
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var post = await _db.BlogPosts
                .FirstOrDefaultAsync(b => b.Id == id);

            if (post == null)
                return NotFound();

            return View(post);
        }

        // CREATE
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPost model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (imageFile != null)
            {
                model.ImagePath = await SaveImage(imageFile);
            }

            var user = await _userManager.GetUserAsync(User);
            model.AuthorId = user?.Id;
            model.CreatedAt = DateTime.UtcNow;

            _db.BlogPosts.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // EDIT
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _db.BlogPosts.FindAsync(id);
            if (post == null)
                return NotFound();

            return View(post);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPost model, IFormFile? imageFile)
        {
            var post = await _db.BlogPosts.FindAsync(id);
            if (post == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            post.Title = model.Title;
            post.ShortDescription = model.ShortDescription;
            post.Content = model.Content;
            post.UpdatedAt = DateTime.UtcNow;

            if (imageFile != null)
            {
                post.ImagePath = await SaveImage(imageFile);
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _db.BlogPosts.FindAsync(id);
            if (post == null)
                return NotFound();

            _db.BlogPosts.Remove(post);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveImage(IFormFile file)
        {
            var uploads = Path.Combine(_env.WebRootPath, "uploads/blog");

            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var path = Path.Combine(uploads, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return "/uploads/blog/" + fileName;
        }
    }
}
