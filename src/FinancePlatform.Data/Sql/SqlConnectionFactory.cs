using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace FinancePlatform.Data.Sql;

public sealed class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    private readonly string _connectionString = connectionString
        ?? throw new ArgumentNullException(nameof(connectionString));

    public DbConnection CreateConnection() => new SqlConnection(_connectionString);
}
