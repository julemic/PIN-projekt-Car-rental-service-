using System.ComponentModel.DataAnnotations;

namespace CarRentalService.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        public string Brand { get; set; } = "";

        [Required]
        public string Model { get; set; } = "";

        public string Category { get; set; } = "";

        [Range(0, 100000, ErrorMessage = "Price must be 0 or higher.")]
        public decimal DailyPrice { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;

        public bool IsActive { get; set; } = true;
       
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;



        [Range(0, 1000, ErrorMessage = "Quantity cannot be negative.")]
        public int TotalQuantity { get; set; }
    }
}
