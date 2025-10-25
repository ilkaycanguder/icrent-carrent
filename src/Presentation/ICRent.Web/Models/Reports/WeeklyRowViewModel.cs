namespace ICRent.Web.Models.Reports
{
    public sealed record WeeklyRowViewModel(
       int VehicleId,               // Araç Id
       string VehicleName,          // Araç Adı
       string VehiclePlate,         // Plaka
       decimal TotalActiveHours,    // Toplam Aktif (saat)
       decimal TotalMaintenanceHours, // Toplam Bakım (saat)
       decimal TotalIdleHours,      // Toplam Boşta (saat)
       decimal ActivePercent,       // Aktif %
       decimal IdlePercent          // Boşta %
   );
}
