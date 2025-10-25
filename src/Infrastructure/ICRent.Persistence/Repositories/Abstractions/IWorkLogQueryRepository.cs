using ICRent.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICRent.Persistence.Repositories.Abstractions
{
    public interface IWorkLogQueryRepository
    {
        Task<List<WorkLog>> GetByVehicleAsync(int vehicleId);
        Task<WorkLog?> GetByIdAsync(int id);
        Task<List<WorkLogRow>> GetRangeWithUserAsync(int[] vehicleIds, DateOnly start, DateOnly end);
        public sealed record WorkLogRow(int VehicleId, string Vehicle, string Plate, DateTime Date,
                                        decimal Active, decimal Maint, string CreatedBy);
        Task<List<WorkLog>> GetByVehicleRangeAsync(int vehicleId, DateOnly start, DateOnly endInclusive);
    }
}
