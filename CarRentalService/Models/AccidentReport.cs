using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalService.Models
{
    public class AccidentReport
    {
        public int Id { get; set; }

        [Required]
        public int RentalId { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // DRIVER INFO
        public string? FullName { get; set; }
        [Required]
        [RegularExpression(@"^\d{11}$",
        ErrorMessage = "OIB must contain exactly 11 digits.")]
        public required string Oib { get; set; } 

        [Required]
        [RegularExpression(@"^\d{8}$",
            ErrorMessage = "Driver license number must contain exactly 8 digits.")]
        public required string DriverLicenseNumber { get; set; } 
        public string? Phone { get; set; }

        // ACCIDENT DETAILS
        public DateTime AccidentDate { get; set; }
        public string? Location { get; set; }
        public string? Weather { get; set; }
        public string? RoadCondition { get; set; }
        public int? Speed { get; set; }
        public bool PoliceNotified { get; set; }

        // DAMAGE DETAILS
        public string? DamageDescription { get; set; }
        public bool OtherPartyInvolved { get; set; }
        public string? OtherPartyName { get; set; }
        public string? OtherPartyPlate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
