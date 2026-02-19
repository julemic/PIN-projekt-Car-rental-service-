using System.ComponentModel.DataAnnotations;
using CarRentalService.Constants;

namespace CarRentalService.Models
{
    public class CarsSearchVm : IValidatableObject
    {
        [Required]
        [Display(Name = "Pickup date & time")]
        public DateTime? Pickup { get; set; }

        [Required]
        [Display(Name = "Return date & time")]
        public DateTime? Return { get; set; }

        public int? VehicleId { get; set; }

        public string Category { get; set; } = VehicleCategories.AllOffers;

        [Required(ErrorMessage = "Please choose insurance.")]
        public InsurancePlan SelectedInsurancePlan { get; set; } = InsurancePlan.Basic;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Pickup.HasValue && Return.HasValue && Return.Value <= Pickup.Value)
            {
                yield return new ValidationResult(
                    "Return date/time must be after pickup date/time.",
                    new[] { nameof(Return) }
                );
            }
        }
    }
}
