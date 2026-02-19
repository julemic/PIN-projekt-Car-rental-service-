namespace CarRentalService.Constants
{
    public static class Roles
    {
        public const string Admin = "Admin";
    }

    public static class TempDataKeys
    {
        public const string Message = "Msg";
    }

    public static class SortOptions
    {
        public const string PriceAsc = "price_asc";
        public const string PriceDesc = "price_desc";
    }

    public static class VehicleCategories
    {
        public const string AllOffers = "All offers";
    }

    public static class InsuranceConfig
    {
        public const decimal BasicPerDay = 0m;
        public const decimal MediumPerDay = 17m;
        public const decimal TotalPerDay = 22m;
        public const decimal DepositRatio = 0.20m;
        public const int FreeCancellationHours = 48;
    }

    public static class Formats
    {
        public const string DateShort = "dd.MM.yyyy";
        public const string DateWithTime = "dd.MM.yyyy HH:mm";
    }

    public static class Upload
    {
        public const string BlogImagePath = "uploads/blog";
        public const string BlogImageUrlPrefix = "/uploads/blog/";
    }
}
