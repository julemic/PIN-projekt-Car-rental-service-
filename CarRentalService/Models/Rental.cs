using System.ComponentModel.DataAnnotations;

namespace CarRentalService.Models
{
    public enum RentalStatus
    {
        Reserved = 0,
        Cancelled = 1,
        Returned = 2,
        Completed = 3
    }

    public enum InsurancePlan
    {
        Basic = 0,
        Medium = 1,
        Total = 2
    }

    public class Rental
    {
        public int Id { get; set; }

        [Required]
        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        [Required]
        public string UserId { get; set; } = "";
        public ApplicationUser? User { get; set; }

        [Required]
        public RentalStatus Status { get; set; } = RentalStatus.Reserved;

        [Required]
        public DateTime Pickup { get; set; }

        [Required]
        public DateTime Return { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReturnedAt { get; set; }

        [Required]
        public InsurancePlan InsurancePlan { get; set; } = InsurancePlan.Basic;

        public decimal DepositPaid { get; set; }
        public decimal FinalAmountPaid { get; set; }

        public bool IsDepositPaid { get; set; }
        public bool IsFullyPaid { get; set; }

        public DateTime? DepositPaidAt { get; set; }
        public DateTime? FullyPaidAt { get; set; }

    }
}
