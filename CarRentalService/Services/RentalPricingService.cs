using CarRentalService.Constants;
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
            ArgumentNullException.ThrowIfNull(vehicle);
            if (returnDate <= pickup)
                throw new ArgumentException("Return date must be after pickup date.");

            var totalDays = (int)Math.Ceiling((returnDate - pickup).TotalDays);
            if (totalDays < 1)
                totalDays = 1;

            decimal insurancePerDay = insurancePlan switch
            {
                InsurancePlan.Basic => InsuranceConfig.BasicPerDay,
                InsurancePlan.Medium => InsuranceConfig.MediumPerDay,
                InsurancePlan.Total => InsuranceConfig.TotalPerDay,
                _ => InsuranceConfig.BasicPerDay
            };

            string insuranceName = insurancePlan switch
            {
                InsurancePlan.Basic => "Basic protect",
                InsurancePlan.Medium => "Medium protect",
                InsurancePlan.Total => "Total protect",
                _ => "Basic protect"
            };

            decimal vehiclePerDay = vehicle.DailyPrice;
            decimal totalPerDay = vehiclePerDay + insurancePerDay;
            decimal totalPrice = totalPerDay * totalDays;
            decimal deposit = Math.Round(totalPrice * InsuranceConfig.DepositRatio, 2);

            var freeCancelUntil = pickup.AddHours(-InsuranceConfig.FreeCancellationHours);
            var canCancel = DateTime.UtcNow <= freeCancelUntil;

            return new RentalPricingResult
            {
                TotalDays = totalDays,
                InsurancePerDay = insurancePerDay,
                TotalPerDay = totalPerDay,
                TotalPrice = totalPrice,
                Deposit = deposit,
                InsuranceName = insuranceName,
                FreeCancellationUntil = freeCancelUntil,
                CanFreeCancel = canCancel
            };
        }
    }
}
