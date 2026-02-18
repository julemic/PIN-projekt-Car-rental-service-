using CarRentalService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarRentalService.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.MigrateAsync();

            
            if (!context.Vehicles.Any())
            {
                context.Vehicles.AddRange(
                    new Vehicle
                    {
                        Brand = "Skoda",
                        Model = "Octavia",
                        Category = "Sedan",
                        DailyPrice = 35,
                        TotalQuantity = 5,
                        ImageUrl = "/images/skoda-octavia.png",
                        IsActive = true
                    },
                    new Vehicle
                    {
                        Brand = "Audi",
                        Model = "A5",
                        Category = "Coupe",
                        DailyPrice = 75,
                        TotalQuantity = 3,
                        ImageUrl = "/images/audi_a5.png",
                        IsActive = true
                    },
                    new Vehicle
                    {
                        Brand = "Volkswagen",
                        Model = "Golf 8",
                        Category = "Hatchback",
                        DailyPrice = 40,
                        TotalQuantity = 6,
                        ImageUrl = "/images/vw-golf-8.png",
                        IsActive = true
                    },
                    new Vehicle
                    {
                        Brand = "Renault",
                        Model = "Clio",
                        Category = "Hatchback",
                        DailyPrice = 30,
                        TotalQuantity = 4,
                        ImageUrl = "/images/renault-clio-2024.png",
                        IsActive = true
                    }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}