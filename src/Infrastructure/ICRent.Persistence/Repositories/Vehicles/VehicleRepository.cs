using ICRent.Domain.Entities;
using ICRent.Persistence.Context;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace ICRent.Persistence.Repositories.Vehicles
{
    public class VehicleRepository
    {
        private readonly IDbConnectionFactory _factory;

        public VehicleRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<int> CreateAsync(string name, string plate, int userId)
        {
            const string sql = """
        INSERT INTO dbo.Vehicles(Name, Plate, CreatedBy)
        VALUES(@n, @p, @u);
        SELECT CAST(SCOPE_IDENTITY() AS INT);
        """;

            using var con = _factory.Create();
            await con.OpenAsync();

            try
            {
                using var cmd = new SqlCommand(sql, con);
                cmd.Parameters.Add("@n", SqlDbType.NVarChar, 60).Value = name;
                cmd.Parameters.Add("@p", SqlDbType.NVarChar, 20).Value = plate;
                cmd.Parameters.Add("@u", SqlDbType.Int).Value = userId;
                return (int)await cmd.ExecuteScalarAsync()!;
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                // 2627: Unique constraint, 2601: Unique index/key duplicate
                throw new InvalidOperationException("Araç adı veya plaka zaten kayıtlı.", ex);
            }
        }


        public async Task<List<Vehicle>> GetAllAsync()
        {
            const string sql = """
                SELECT v.Id, v.Name, v.Plate, v.CreatedAt,
                       v.UpdatedBy, v.UpdatedAt,
                       c.UserName AS CreatedByName,
                       u.UserName AS UpdatedByName
                FROM dbo.Vehicles v
                LEFT JOIN dbo.Users c ON v.CreatedBy = c.Id
                LEFT JOIN dbo.Users u ON v.UpdatedBy = u.Id
                ORDER BY v.Id;
            """;

            using var con = _factory.Create();
            await con.OpenAsync();

            using var cmd = new SqlCommand(sql, con);
            using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<Vehicle>();
            while (await reader.ReadAsync())
            {
                list.Add(new Vehicle
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Plate = reader.GetString(2),
                    CreatedAt = reader.GetDateTime(3),
                    UpdatedBy = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    UpdatedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    CreatedByName = reader.IsDBNull(6) ? "-" : reader.GetString(6),
                    UpdatedByName = reader.IsDBNull(7) ? "-" : reader.GetString(7)
                });
            }

            return list;
        }

        public async Task<Vehicle?> GetByIdAsync(int id)
        {
            const string sql = """
                    SELECT Id, Name, Plate, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt
                    FROM dbo.Vehicles WHERE Id = @i;
                    """;

            using var con = _factory.Create();
            await con.OpenAsync();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@i", SqlDbType.Int).Value = id;
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
                return null;

            return new Vehicle
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                Plate = r.GetString(2),
                CreatedBy = r.GetInt32(3),
                CreatedAt = r.GetDateTime(4),
                UpdatedBy = r.IsDBNull(5) ? null : r.GetInt32(5),
                UpdatedAt = r.IsDBNull(6) ? null : r.GetDateTime(6)
            };
        }

        public async Task UpdateAsync(int id, string name, string plate, int updatedBy)
        {
            const string sql = """
            UPDATE dbo.Vehicles
               SET Name=@n, Plate=@p, UpdatedBy=@u, UpdatedAt=SYSUTCDATETIME()
             WHERE Id=@i;
            """;

            using var con = _factory.Create();
            await con.OpenAsync();

            try
            {
                using var cmd = new SqlCommand(sql, con);
                cmd.Parameters.Add("@n", SqlDbType.NVarChar, 60).Value = name;
                cmd.Parameters.Add("@p", SqlDbType.NVarChar, 20).Value = plate;
                cmd.Parameters.Add("@u", SqlDbType.Int).Value = updatedBy;
                cmd.Parameters.Add("@i", SqlDbType.Int).Value = id;
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                throw new InvalidOperationException("Araç adı veya plaka zaten kayıtlı.", ex);
            }
        }


        public async Task DeleteAsync(int id)
        {
            const string sql = "DELETE FROM dbo.Vehicles WHERE Id=@i;";
            using var con = _factory.Create();
            await con.OpenAsync();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@i", SqlDbType.Int).Value = id;
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            const string sql = @"SELECT 1 FROM dbo.Vehicles WHERE Name = @n AND (@i IS NULL OR Id <> @i);";
            using var con = _factory.Create();
            await con.OpenAsync();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@n", SqlDbType.NVarChar, 60).Value = name;
            cmd.Parameters.Add("@i", SqlDbType.Int).Value = (object?)excludeId ?? DBNull.Value;
            using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync();
        }

        public async Task<bool> ExistsByPlateAsync(string plate, int? excludeId = null)
        {
            const string sql = @"SELECT 1 FROM dbo.Vehicles WHERE Plate = @p AND (@i IS NULL OR Id <> @i);";
            using var con = _factory.Create();
            await con.OpenAsync();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@p", SqlDbType.NVarChar, 20).Value = plate;
            cmd.Parameters.Add("@i", SqlDbType.Int).Value = (object?)excludeId ?? DBNull.Value;
            using var r = await cmd.ExecuteReaderAsync();
            return await r.ReadAsync();
        }

    }
}