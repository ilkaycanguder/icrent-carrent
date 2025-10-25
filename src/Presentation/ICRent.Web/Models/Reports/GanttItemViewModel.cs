namespace ICRent.Web.Models.Reports
{
    public class GanttItemViewModel
    {
        public string VehicleName { get; set; } = null!;
        public string Plate { get; set; } = null!;
        public DateOnly WorkDate { get; set; }      // Gün bilgisi
        public decimal ActiveHours { get; set; }    // Saat
        public decimal MaintenanceHours { get; set; } // Saat
        public string EnteredBy { get; set; } = null!;
    }
}
