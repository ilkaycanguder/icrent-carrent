using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICRent.Persistence.Context
{
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Sql") ?? throw new InvalidOperationException("Connection string 'Sql' not found.");
        }

        public SqlConnection Create()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
