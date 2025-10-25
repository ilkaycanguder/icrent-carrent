namespace ICRent.Domain.Entities
{
    public sealed class Vehicle
    {
        public int Id { get; init; }
        public string Name { get; init; } = null!;
        public string Plate { get; init; } = null!;
        public int CreatedBy { get; init; }
        public DateTime CreatedAt { get; init; }
        public int? UpdatedBy { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? CreatedByName { get; set; }
        public string? UpdatedByName { get; set; }

    }

}
