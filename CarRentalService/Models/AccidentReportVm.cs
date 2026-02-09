using System.ComponentModel.DataAnnotations;

namespace CarRentalService.Models
{
    public class AccidentReportVm
    {
        [Required]
        public int RentalId { get; set; }

        public int VehicleId { get; set; }
        public string VehicleBrand { get; set; } = "";
        public string VehicleModel { get; set; } = "";
        public string VehicleCategory { get; set; } = "";

        [Required, StringLength(80)]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [StringLength(30)]
        public string Phone { get; set; } = "";

        [Required]
        [Display(Name = "Date of accident")]
        public DateTime AccidentDate { get; set; } = DateTime.Now;

        [Required, StringLength(120)]
        public string Location { get; set; } = "";

        [Required, StringLength(1200)]
        public string Description { get; set; } = "";

        public bool PoliceReported { get; set; }

        [StringLength(400)]
        public string? OtherParty { get; set; }
    }
}
