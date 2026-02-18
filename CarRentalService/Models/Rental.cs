using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

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

        // ==============================
        // CALCULATED PROPERTIES
        // ==============================

        public static decimal GetInsurancePricePerDay(InsurancePlan plan)
        {
            return plan switch
            {
                InsurancePlan.Basic => 0m,
                InsurancePlan.Medium => 17m,
                InsurancePlan.Total => 22m,
                _ => 0m
            };
        }

        [NotMapped]
        public int TotalDays
        {
            get
            {
                var days = (int)Math.Ceiling((Return - Pickup).TotalDays);
                return days < 1 ? 1 : days;
            }
        }

        [NotMapped]
        public decimal VehiclePricePerDay =>
            Vehicle?.DailyPrice ?? 0m;

        [NotMapped]
        public decimal InsurancePricePerDay =>
            GetInsurancePricePerDay(InsurancePlan);

        [NotMapped]
        public decimal TotalPricePerDay =>
            VehiclePricePerDay + InsurancePricePerDay;

        [NotMapped]
        public decimal TotalPrice =>
            TotalDays * TotalPricePerDay;

        [NotMapped]
        public decimal DepositAmount =>
            Math.Round(TotalPrice * 0.20m, 2);

        [NotMapped]
        public DateTime FreeCancellationUntil =>
            Pickup.AddHours(-48);

        [NotMapped]
        public bool CanFreeCancel =>
            Status == RentalStatus.Reserved &&
            DateTime.UtcNow <= FreeCancellationUntil;

        [NotMapped]
        public string InsuranceName => InsurancePlan switch
        {
            InsurancePlan.Basic => "Basic protect",
            InsurancePlan.Medium => "Medium protect",
            InsurancePlan.Total => "Total protect",
            _ => "Basic protect"
        };

        public decimal DepositPaid { get; set; }
        public decimal FinalAmountPaid { get; set; }

        public bool IsDepositPaid { get; set; }
        public bool IsFullyPaid { get; set; }

        public DateTime? DepositPaidAt { get; set; }
        public DateTime? FullyPaidAt { get; set; }

    }
}
