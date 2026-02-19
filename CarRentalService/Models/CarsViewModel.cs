namespace CarRentalService.Models
{
    public class CarsViewModel
    {
        public CarsSearchVm Search { get; set; } = new();
        public bool ShowVehicles { get; set; }
        public List<VehicleAvailability> Vehicles { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string SelectedCategory { get; set; } = "";
        public string Sort { get; set; } = "";
    }

    public class VehicleAvailability
    {
        public Vehicle Vehicle { get; set; } = null!;
        public int Available { get; set; }
    }
}
