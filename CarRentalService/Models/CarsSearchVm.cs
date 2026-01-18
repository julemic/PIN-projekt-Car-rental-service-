using System.ComponentModel.DataAnnotations;

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

        public string Category { get; set; } = "All offers";


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Pickup.HasValue && Return.HasValue && Return.Value < Pickup.Value)
            {
                yield return new ValidationResult(
                    "Return date/time cannot be earlier than pickup date/time.",
                    new[] { nameof(Return) }
                );
            }
        }
    }
}
