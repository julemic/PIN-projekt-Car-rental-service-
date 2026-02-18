using CarRentalService.Models;

namespace CarRentalService.Services
{
    public class RentalPricingService : IRentalPricingService
    {
        public RentalPricingResult CalculatePricing(
            Vehicle vehicle,
            DateTime pickup,
            DateTime returnDate,
            InsurancePlan insurancePlan)
        {
            var totalDays = (int)Math.Ceiling((returnDate - pickup).TotalDays);
            if (totalDays < 1)
                totalDays = 1;

            decimal insurancePerDay = insurancePlan switch
            {
                InsurancePlan.Basic => 0m,
                InsurancePlan.Medium => 17m,
                InsurancePlan.Total => 22m,
                _ => 0m
            };

            decimal vehiclePerDay = vehicle.DailyPrice;
            decimal totalPerDay = vehiclePerDay + insurancePerDay;
            decimal totalPrice = totalPerDay * totalDays;
            decimal deposit = Math.Round(totalPrice * 0.20m, 2);

            var freeCancelUntil = pickup.AddHours(-48);
            var canCancel = DateTime.UtcNow <= freeCancelUntil;

            return new RentalPricingResult
            {
                TotalDays = totalDays,
                InsurancePerDay = insurancePerDay,
                TotalPerDay = totalPerDay,
                TotalPrice = totalPrice,
                Deposit = deposit,
                FreeCancellationUntil = freeCancelUntil,
                CanFreeCancel = canCancel
            };
        }
    }
}
