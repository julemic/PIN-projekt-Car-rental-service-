using CarRentalService.Data;
using CarRentalService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarRentalService.Constants;

namespace CarRentalService.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public BlogController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _db = db;
            _userManager = userManager;
            _env = env;
            _config = config;
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
        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["TinyMceApiKey"] = _config["TinyMce:ApiKey"];
            return View();
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPost model, IFormFile? imageFile)
        {
            ViewData["TinyMceApiKey"] = _config["TinyMce:ApiKey"];

            if (!ModelState.IsValid)
                return View(model);

            if (imageFile != null)
            {
                var path = await SaveImage(imageFile);
                if (path == null)
                {
                    ModelState.AddModelError("", "Invalid image. Allowed: .jpg, .jpeg, .png, .webp (max 5MB).");
                    return View(model);
                }
                model.ImagePath = path;
            }

            var user = await _userManager.GetUserAsync(User);
            model.AuthorId = user?.Id;
            model.CreatedAt = DateTime.UtcNow;

            _db.BlogPosts.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // EDIT
        [Authorize(Roles = Roles.Admin)]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _db.BlogPosts.FindAsync(id);
            if (post == null)
                return NotFound();

            ViewData["TinyMceApiKey"] = _config["TinyMce:ApiKey"];
            return View(post);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPost model, IFormFile? imageFile)
        {
            ViewData["TinyMceApiKey"] = _config["TinyMce:ApiKey"];

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
                var path = await SaveImage(imageFile);
                if (path == null)
                {
                    ModelState.AddModelError("", "Invalid image. Allowed: .jpg, .jpeg, .png, .webp (max 5MB).");
                    return View(model);
                }
                DeleteImage(post.ImagePath);
                post.ImagePath = path;
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Index));

            var post = await _db.BlogPosts.FindAsync(id);
            if (post == null)
                return NotFound();

            _db.BlogPosts.Remove(post);
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

            var uploads = Path.Combine(_env.WebRootPath, Upload.BlogImagePath);

            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid() + ext;
            var path = Path.Combine(uploads, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return Upload.BlogImageUrlPrefix + fileName;
        }

        private void DeleteImage(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return;

            var fullPath = Path.Combine(_env.WebRootPath, imagePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}
