using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CarRentalService.Models
{
    public class ApplicationUser : IdentityUser, IValidatableObject
    {
        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(100)]
        public string? ResidenceAddress { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? Nationality { get; set; }

        [RegularExpression(@"^\d{11}$",
            ErrorMessage = "OIB must contain exactly 11 digits.")]
        public string? Oib { get; set; }

        [RegularExpression(@"^\d{8}$",
            ErrorMessage = "Driver license number must contain exactly 8 digits.")]
        public string? DriverLicenseNumber { get; set; }

        [RegularExpression(@"^[A-Za-z0-9]{9}$",
            ErrorMessage = "ID card number must contain exactly 9 characters.")]
        public string? IdCardNumber { get; set; }

        public bool IsVerified { get; set; } = false;
        public DateTime? VerifiedAt { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateOfBirth.HasValue)
            {
                var today = DateTime.Today;
                var minAllowedDate = today.AddYears(-18);

                if (DateOfBirth.Value > minAllowedDate)
                {
                    yield return new ValidationResult(
                        "You must be at least 18 years old.",
                        new[] { nameof(DateOfBirth) });
                }
            }
        }
    }
}
