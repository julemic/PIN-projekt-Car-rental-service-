using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CarRentalService.Models;

namespace CarRentalService.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Rental> Rentals => Set<Rental>();

        public DbSet<AccidentReport> AccidentReports { get; set; }

        public DbSet<BlogPost> BlogPosts { get; set; }

    }
}
