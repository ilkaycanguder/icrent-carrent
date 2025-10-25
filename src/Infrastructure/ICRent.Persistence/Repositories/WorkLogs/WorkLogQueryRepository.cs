using ICRent.Domain.Entities;
using ICRent.Persistence.Context;
using ICRent.Persistence.Repositories.Abstractions;
using Microsoft.Data.SqlClient;
using System.Data;
using static ICRent.Persistence.Repositories.Abstractions.IWorkLogQueryRepository;

namespace ICRent.Persistence.Repositories.WorkLogs
{
    public class WorkLogQueryRepository : IWorkLogQueryRepository
    {
        private readonly IDbConnectionFactory _f;
        public WorkLogQueryRepository(IDbConnectionFactory f) => _f = f;

        public async Task<WorkLog?> GetByIdAsync(int id)
        {
            const string sql = """
            SELECT Id,VehicleId,WorkDate,ActiveHours,MaintenanceHours,CreatedBy,CreatedAt,UpdatedBy,UpdatedAt
            FROM dbo.WorkLogs WHERE Id=@id;
            """;
            await using var con = _f.Create();
            await con.OpenAsync();
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            return new WorkLog
            {
                Id = r.GetInt32(0),
                VehicleId = r.GetInt32(1),
                WorkDate = DateOnly.FromDateTime(r.GetDateTime(2)),
                ActiveHours = r.GetDecimal(3),
                MaintenanceHours = r.GetDecimal(4),
                CreatedBy = r.GetInt32(5),
                CreatedAt = r.GetDateTime(6),
                UpdatedBy = r.IsDBNull(7) ? null : r.GetInt32(7),
                UpdatedAt = r.IsDBNull(8) ? null : r.GetDateTime(8)
            };
        }

        public async Task<List<WorkLog>> GetByVehicleAsync(int vehicleId)
        {
            const string sql = """
        SELECT Id,VehicleId,WorkDate,ActiveHours,MaintenanceHours,CreatedBy,CreatedAt,UpdatedBy,UpdatedAt
        FROM dbo.WorkLogs WHERE VehicleId=@v ORDER BY WorkDate DESC;
        """;
            await using var con = _f.Create();
            await con.OpenAsync();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@v", SqlDbType.Int).Value = vehicleId;

            using var r = await cmd.ExecuteReaderAsync();
            var list = new List<WorkLog>();
            while (await r.ReadAsync())
            {
                list.Add(new WorkLog
                {
                    Id = r.GetInt32(0),
                    VehicleId = r.GetInt32(1),
                    WorkDate = DateOnly.FromDateTime(r.GetDateTime(2)),
                    ActiveHours = r.GetDecimal(3),
                    MaintenanceHours = r.GetDecimal(4),
                    CreatedBy = r.GetInt32(5),
                    CreatedAt = r.GetDateTime(6),
                    UpdatedBy = r.IsDBNull(7) ? null : r.GetInt32(7),
                    UpdatedAt = r.IsDBNull(8) ? null : r.GetDateTime(8)
                });
            }
            return list;
        }

        public async Task<List<WorkLogRow>> GetRangeWithUserAsync(
            int[] vehicleIds, DateOnly start, DateOnly end)
        {
            var names = vehicleIds.Select((_, i) => $"@id{i}").ToArray();
            var sql = $@"
            SELECT v.Id, v.Name, v.Plate, w.WorkDate, w.ActiveHours, w.MaintenanceHours, u.UserName
            FROM dbo.WorkLogs w
            JOIN dbo.Vehicles v ON v.Id = w.VehicleId
            JOIN dbo.Users    u ON u.Id = w.CreatedBy
            WHERE w.VehicleId IN ({string.Join(",", names)}) 
              AND w.WorkDate >= @s AND w.WorkDate < @e
            ORDER BY v.Id, w.WorkDate;";

            await using var con = _f.Create();
            await con.OpenAsync();
            using var cmd = new SqlCommand(sql, con);

            for (int i = 0; i < vehicleIds.Length; i++)
                cmd.Parameters.Add(names[i], SqlDbType.Int).Value = vehicleIds[i];

            cmd.Parameters.Add("@s", SqlDbType.Date).Value = start.ToDateTime(TimeOnly.MinValue);
            cmd.Parameters.Add("@e", SqlDbType.Date).Value = end.ToDateTime(TimeOnly.MinValue);

            using var r = await cmd.ExecuteReaderAsync();
            var list = new List<WorkLogRow>();
            while (await r.ReadAsync())
            {
                list.Add(new WorkLogRow(
                   r.GetInt32(0), r.GetString(1), r.GetString(2),
                   r.GetDateTime(3), r.GetDecimal(4), r.GetDecimal(5), r.GetString(6)
                ));
            }

            return list;
        }

        public async Task<List<WorkLog>> GetByVehicleRangeAsync(int vehicleId, DateOnly start, DateOnly endInclusive)
        {
            const string sql = """
            SELECT Id, VehicleId, WorkDate, ActiveHours, MaintenanceHours, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt
            FROM dbo.WorkLogs
            WHERE VehicleId = @v
              AND WorkDate >= @s
              AND WorkDate  < @e   -- endExclusive
            ORDER BY WorkDate DESC;
            """;

            var endExclusive = endInclusive.AddDays(1); // [start, endInclusive] için

            await using var con = _f.Create();  
            await con.OpenAsync();
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@v", SqlDbType.Int).Value = vehicleId;
            cmd.Parameters.Add("@s", SqlDbType.Date).Value = start.ToDateTime(TimeOnly.MinValue);
            cmd.Parameters.Add("@e", SqlDbType.Date).Value = endExclusive.ToDateTime(TimeOnly.MinValue);

            var list = new List<WorkLog>();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new WorkLog
                {
                    Id = r.GetInt32(0),
                    VehicleId = r.GetInt32(1),
                    WorkDate = DateOnly.FromDateTime(r.GetDateTime(2)),
                    ActiveHours = r.GetDecimal(3),
                    MaintenanceHours = r.GetDecimal(4),
                    CreatedBy = r.GetInt32(5),
                    CreatedAt = r.GetDateTime(6),
                    UpdatedBy = r.IsDBNull(7) ? null : r.GetInt32(7),
                    UpdatedAt = r.IsDBNull(8) ? null : r.GetDateTime(8)
                });
            }
            return list;
        }


    }
}
