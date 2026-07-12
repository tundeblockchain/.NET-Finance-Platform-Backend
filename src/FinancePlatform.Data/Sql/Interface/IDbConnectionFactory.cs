using System.Data.Common;

namespace FinancePlatform.Data.Sql;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
