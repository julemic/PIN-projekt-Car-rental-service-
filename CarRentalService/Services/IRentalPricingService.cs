using CarRentalService.Models;

namespace CarRentalService.Services
{
    public interface IRentalPricingService
    {
        RentalPricingResult CalculatePricing(
            Vehicle vehicle,
            DateTime pickup,
            DateTime returnDate,
            InsurancePlan insurancePlan);
    }
}
