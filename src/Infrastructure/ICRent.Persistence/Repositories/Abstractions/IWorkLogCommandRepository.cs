using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICRent.Persistence.Repositories.Abstractions
{
    public interface IWorkLogCommandRepository
    {
        Task UpsertAsync(int vehicleId, DateOnly date, decimal active, decimal maintenance, int userId);
        Task DeleteAsync(int id, int userId);   
    }
}
