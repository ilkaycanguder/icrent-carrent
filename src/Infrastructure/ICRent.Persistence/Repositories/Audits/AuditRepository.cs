using ICRent.Domain.Entities;
using ICRent.Persistence.Context;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICRent.Persistence.Repositories.Audits
{
    public class AuditRepository
    {
        private readonly IDbConnectionFactory _factory;

        public AuditRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task LogAsync(int userId, string action, string entity, int entityId, string? details = null)
        {
            const string sql = """
            INSERT INTO dbo.AuditLogs(UserId,Action,Entity,EntityId,Details)
            VALUES(@u,@a,@e,@i,@d);
            """;
            using var con = _factory.Create(); await con.OpenAsync();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@a", action);
            cmd.Parameters.AddWithValue("@e", entity);
            cmd.Parameters.AddWithValue("@i", entityId);
            cmd.Parameters.AddWithValue("@d", (object?)details ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<(long Id, string User, string Action, string Entity, int EntityId, string? Details, DateTime CreatedAt)>> GetLatestAsync(int top = 100)
        {
            const string sql = @"
              SELECT TOP (@top)
                     a.Id, u.UserName AS [User], a.Action, a.Entity, a.EntityId, a.Details, a.CreatedAt
              FROM dbo.AuditLogs AS a
              INNER JOIN dbo.Users AS u ON u.Id = a.UserId
              ORDER BY a.Id DESC;";

            using var con = _factory.Create(); await con.OpenAsync();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@top", top);
            using var r = await cmd.ExecuteReaderAsync();

            var list = new List<(long, string, string, string, int, string?, DateTime)>();
            while (await r.ReadAsync())
                list.Add((r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3),
                          r.GetInt32(4), r.IsDBNull(5) ? null : r.GetString(5), r.GetDateTime(6)));
            return list;
        }

        public sealed record WorkLogAuditDto(long Id, string User, string Action, int EntityId, string? Details, DateTime CreatedAt);

        public async Task<List<WorkLogAuditDto>> GetWorkLogHistoryAsync(
            int? vehicleId, DateOnly? start, DateOnly? end, int top = 50)
        {
                    const string sql = """
            SELECT TOP (@top) a.Id, u.UserName, a.Action, a.EntityId, a.Details, a.CreatedAt
            FROM dbo.AuditLogs a
            JOIN dbo.Users u ON u.Id = a.UserId
            WHERE a.Entity = N'WorkLogs'
              AND (@veh IS NULL OR a.EntityId = @veh)
              AND (@s  IS NULL OR a.CreatedAt >= @s)
              AND (@e  IS NULL OR a.CreatedAt <  @e)
            ORDER BY a.Id DESC;
            """;
            await using var con = _factory.Create();
            await con.OpenAsync();
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@top", top);
            cmd.Parameters.AddWithValue("@veh", (object?)vehicleId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@s", (object?)start?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@e", (object?)end?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
            using var r = await cmd.ExecuteReaderAsync();
            var list = new List<WorkLogAuditDto>();
            while (await r.ReadAsync())
                list.Add(new WorkLogAuditDto(r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetInt32(3),
                                             r.IsDBNull(4) ? null : r.GetString(4), r.GetDateTime(5)));
            return list;
        }

        // ICRent.Persistence.Repositories.Audits.AuditRepository
        public async Task<List<AuditLog>> GetFilteredAsync(
            string? entity,         // "Araclar" / "Is Kayitlari" vs. (NULL=hepsi)
            string? action,         // "Ekleme" / "Guncelleme" / "Silme" / "Merge" vs. (NULL=hepsi)
            int? userId,            // Kaydeden kullanici (NULL=hepsi)
            DateTime? start,        // >= start (NULL=limitsiz)
            DateTime? end,          // <  end   (NULL=limitsiz)
            string? q,              // Serbest metin (Details/EntityId/UserName’de LIKE)
            int top = 200)
        {
            const string sqlBase = """
    SELECT TOP (@top)
           a.Id, a.UserId, a.Action, a.Entity, a.EntityId, a.Details, a.CreatedAt,
           u.UserName
    FROM dbo.AuditLogs a
    LEFT JOIN dbo.Users u ON u.Id = a.UserId
    WHERE 1=1
    """;

            var sb = new System.Text.StringBuilder(sqlBase);

            if (!string.IsNullOrWhiteSpace(entity)) sb.AppendLine("  AND a.Entity = @entity");
            if (!string.IsNullOrWhiteSpace(action)) sb.AppendLine("  AND a.Action = @action");
            if (userId.HasValue) sb.AppendLine("  AND a.UserId = @userId");
            if (start.HasValue) sb.AppendLine("  AND a.CreatedAt >= @start");
            if (end.HasValue) sb.AppendLine("  AND a.CreatedAt <  @end");
            if (!string.IsNullOrWhiteSpace(q))
            {
                // Details + EntityId + UserName içinde LIKE
                sb.AppendLine("  AND (a.Details LIKE '%' + @q + '%' OR");
                sb.AppendLine("       CONVERT(nvarchar(32), a.EntityId) LIKE '%' + @q + '%' OR");
                sb.AppendLine("       u.UserName LIKE '%' + @q + '%')");
            }

            sb.AppendLine("ORDER BY a.CreatedAt DESC;");

            await using var con = _factory.Create();
            await con.OpenAsync();
            await using var cmd = new SqlCommand(sb.ToString(), con);

            cmd.Parameters.Add("@top", SqlDbType.Int).Value = top;

            if (!string.IsNullOrWhiteSpace(entity)) cmd.Parameters.Add("@entity", SqlDbType.NVarChar, 64).Value = entity!;
            if (!string.IsNullOrWhiteSpace(action)) cmd.Parameters.Add("@action", SqlDbType.NVarChar, 32).Value = action!;
            if (userId.HasValue) cmd.Parameters.Add("@userId", SqlDbType.Int).Value = userId.Value;
            if (start.HasValue) cmd.Parameters.Add("@start", SqlDbType.DateTime2).Value = start.Value;
            if (end.HasValue) cmd.Parameters.Add("@end", SqlDbType.DateTime2).Value = end.Value;
            if (!string.IsNullOrWhiteSpace(q)) cmd.Parameters.Add("@q", SqlDbType.NVarChar, 200).Value = q!;

            var list = new List<AuditLog>();
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new AuditLog
                {
                    Id = r.GetInt32(0),
                    UserId = r.GetInt32(1),
                    Action = r.GetString(2),
                    Entity = r.GetString(3),
                    EntityId = r.GetInt32(4),
                    Details = r.IsDBNull(5) ? null : r.GetString(5),
                    CreatedAt = r.GetDateTime(6),
                    UserName = r.IsDBNull(7) ? null : r.GetString(7)
                });
            }
            return list;
        }

    }
}
