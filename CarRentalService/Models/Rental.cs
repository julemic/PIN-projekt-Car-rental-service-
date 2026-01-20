using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CarRentalService.Models
{
    public class Rental
    {
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        [Required]
        public DateTime Pickup { get; set; }

        [Required]
        public DateTime Return { get; set; }

        
        [Required]
        public string UserId { get; set; } = "";
        public IdentityUser? User { get; set; }

        public bool IsReturned { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReturnedAt { get; set; }
    }
}
