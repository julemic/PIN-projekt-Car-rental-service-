namespace CarRentalService.Models
{
    public class RentalPricingResult
    {
        public int TotalDays { get; set; }
        public decimal InsurancePerDay { get; set; }
        public decimal TotalPerDay { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Deposit { get; set; }
        public DateTime FreeCancellationUntil { get; set; }
        public bool CanFreeCancel { get; set; }
    }
}
