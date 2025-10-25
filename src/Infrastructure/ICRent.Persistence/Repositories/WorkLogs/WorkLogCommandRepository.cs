using ICRent.Persistence.Context;
using ICRent.Persistence.Repositories.Abstractions;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ICRent.Persistence.Repositories.WorkLogs
{
    public class WorkLogCommandRepository : IWorkLogCommandRepository
    {
        private readonly IDbConnectionFactory _factory;

        public WorkLogCommandRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task DeleteAsync(int id, int userId)
        {
            const string sql = "DELETE FROM dbo.WorkLogs WHERE Id=@id;";
            await using var con = _factory.Create();
            await con.OpenAsync();
            await using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpsertAsync(int vehicleId, DateOnly date, decimal active, decimal maintenance, int userId)
        {
            // ŞART: toplam 24’ü aşmasın (ekran doğrulaması kaçarsa diye)
            if (active < 0 || maintenance < 0 || active + maintenance > 24)
                throw new InvalidOperationException("Bir gün için toplam (Aktif + Bakım) 24 saati geçemez.");

            const string sql = """
            MERGE dbo.WorkLogs AS t
            USING (SELECT @v AS VehicleId, @d AS WorkDate) AS s
              ON (t.VehicleId = s.VehicleId AND t.WorkDate = s.WorkDate)

            -- VAR OLAN KAYITTA: yeni giriş önceki toplamla 24’ü aşmayacaksa güncelle
            WHEN MATCHED AND (t.ActiveHours + t.MaintenanceHours + @a + @m) <= 24 THEN
              UPDATE SET
                ActiveHours      = t.ActiveHours      + @a,
                MaintenanceHours = t.MaintenanceHours + @m,
                UpdatedBy = @u,
                UpdatedAt = SYSUTCDATETIME()

            -- YENİ KAYITTA: (a+m) ≤ 24 ise ekle
            WHEN NOT MATCHED AND (@a + @m) <= 24 THEN
              INSERT (VehicleId, WorkDate, ActiveHours, MaintenanceHours, CreatedBy)
              VALUES (@v, @d, @a, @m, @u);

            -- Kaç satır etkilendiğini döndür
            SELECT @@ROWCOUNT;
            """;

            await using var con = _factory.Create();
            await con.OpenAsync();
            await using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.Add("@v", SqlDbType.Int).Value = vehicleId;
            cmd.Parameters.Add("@d", SqlDbType.Date).Value = date.ToDateTime(TimeOnly.MinValue);

            var pA = cmd.Parameters.Add("@a", SqlDbType.Decimal);
            pA.Precision = 5; pA.Scale = 2; pA.Value = active;

            var pM = cmd.Parameters.Add("@m", SqlDbType.Decimal);
            pM.Precision = 5; pM.Scale = 2; pM.Value = maintenance;

            cmd.Parameters.Add("@u", SqlDbType.Int).Value = userId;

            // MERGE sonunda SELECT @@ROWCOUNT; var, onu oku
            var affectedObj = await cmd.ExecuteScalarAsync();
            var affected = Convert.ToInt32(affectedObj ?? 0);

            if (affected == 0)
                throw new InvalidOperationException("Bir gün için toplam (Aktif + Bakım) 24 saati geçemez.");
        }

    }
}
