using ICRent.Domain.Entities;
using ICRent.Persistence.Context;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICRent.Persistence.Repositories.Users
{
    public class UserRepository
    {
        private readonly IDbConnectionFactory _factory;

        public UserRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            const string sql = """
            SELECT Id, UserName, PasswordHash, PasswordSalt, Role, CreatedAt
            FROM dbo.Users WHERE UserName=@u;
            """;

            using var con = _factory.Create();
            await con.OpenAsync();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@u", userName);

            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;

            return new User
            {
                Id = r.GetInt32(0),
                UserName = r.GetString(1),
                PasswordHash = (byte[])r["PasswordHash"],
                PasswordSalt = (byte[])r["PasswordSalt"],
                Role = r.GetString(4),
                CreatedAt = r.GetDateTime(5)
            };
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            const string sql = """
                SELECT Id, UserName, PasswordHash, PasswordSalt, Role, CreatedAt
                FROM dbo.Users WHERE Id=@i;
                """;

            using var con = _factory.Create();
            await con.OpenAsync();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@i", id);

            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;

            return new User
            {
                Id = r.GetInt32(0),
                UserName = r.GetString(1),
                PasswordHash = (byte[])r["PasswordHash"],
                PasswordSalt = (byte[])r["PasswordSalt"],
                Role = r.GetString(4),
                CreatedAt = r.GetDateTime(5)
            };
        }

        // Kullanıcı adı benzer mi? (güncellemede kendi Id'sini hariç tut)
        public async Task<bool> IsUserNameTakenAsync(string userName, int excludeUserId)
        {
            const string sql = "SELECT COUNT(1) FROM dbo.Users WHERE UserName=@u AND Id<>@i;";
            using var con = _factory.Create();
            await con.OpenAsync();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@u", userName);
            cmd.Parameters.AddWithValue("@i", excludeUserId);
            var count = (int)await cmd.ExecuteScalarAsync();
            return count > 0;
        }

        // Username + Password birlikte güncelle
        public async Task UpdateCredentialsAsync(int userId, string newUserName, byte[]? newHash, byte[]? newSalt)
        {
            const string sql = """
                UPDATE dbo.Users
                SET UserName=@u, PasswordHash=@h, PasswordSalt=@s
                WHERE Id=@i;
                """;

            using var con = _factory.Create();
            await con.OpenAsync();
            using var cmd = new SqlCommand(sql, con);

            cmd.Parameters.AddWithValue("@u", newUserName);

            // varbinary parametreleri boyutlu tanımlamak daha sağlıklı
            var pHash = new SqlParameter("@h", SqlDbType.VarBinary, newHash.Length) { Value = newHash };
            var pSalt = new SqlParameter("@s", SqlDbType.VarBinary, newSalt.Length) { Value = newSalt };
            cmd.Parameters.Add(pHash);
            cmd.Parameters.Add(pSalt);

            cmd.Parameters.AddWithValue("@i", userId);

            await cmd.ExecuteNonQueryAsync();
        }
     
    }
}
