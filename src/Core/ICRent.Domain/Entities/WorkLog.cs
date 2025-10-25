namespace ICRent.Domain.Entities
{
    public sealed class WorkLog
    {
        public int Id { get; init; }
        public int VehicleId { get; init; }
        public DateOnly WorkDate { get; init; }           // DATE
        public decimal ActiveHours { get; init; }         // DECIMAL(5,2)
        public decimal MaintenanceHours { get; init; }    // DECIMAL(5,2)
        public int CreatedBy { get; init; }
        public DateTime CreatedAt { get; init; }
        public int? UpdatedBy { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }
}
