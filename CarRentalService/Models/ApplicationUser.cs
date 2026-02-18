using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CarRentalService.Models
{
    public class ApplicationUser : IdentityUser
    {
        

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        [Range(typeof(DateTime), "1940-01-01", "2100-01-01",ErrorMessage = "Date of birth must be after 01.01.1940.")]
        public DateTime DateOfBirth { get; set; }


        [Required]
        [MaxLength(100)]
        public string ResidenceAddress { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string City { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Nationality { get; set; } = null!;

        

        [Required]
        [RegularExpression(@"^\d{11}$",
            ErrorMessage = "OIB must contain exactly 11 digits.")]
        public string Oib { get; set; } = null!;

        [Required]
        [RegularExpression(@"^\d{8}$",
            ErrorMessage = "Driver license number must contain exactly 8 digits.")]
        public string DriverLicenseNumber { get; set; } = null!;

        [Required]
        [RegularExpression(@"^[A-Za-z0-9]{9}$",
            ErrorMessage = "ID card number must contain exactly 9 characters.")]
        public string IdCardNumber { get; set; } = null!;

        

        public bool IsVerified { get; set; } = false;

        public DateTime? VerifiedAt { get; set; }
    }
}
