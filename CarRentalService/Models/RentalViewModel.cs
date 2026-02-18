using System;
using CarRentalService.Models;

namespace CarRentalService.Models
{
    public class RentalViewModel
    {
        public int Id { get; set; }

        // Vehicle info
        public int VehicleId { get; set; }
        public string Brand { get; set; } = "";
        public string Model { get; set; } = "";
        public string Category { get; set; } = "";

        public decimal DailyPrice { get; set; }
        public int TotalQuantity { get; set; }

        public DateTime? ReturnedAt { get; set; }


        // Rental info
        public RentalStatus Status { get; set; }
        public DateTime Pickup { get; set; }
        public DateTime Return { get; set; }

        // Pricing
        public string InsuranceName { get; set; } = "";
        public decimal InsurancePricePerDay { get; set; }
        public int TotalDays { get; set; }
        public decimal TotalPricePerDay { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }

        // Cancellation
        public DateTime FreeCancellationUntil { get; set; }
        public bool CanFreeCancel { get; set; }
    }
}
