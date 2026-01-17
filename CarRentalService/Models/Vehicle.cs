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

        public decimal DailyPrice { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}
