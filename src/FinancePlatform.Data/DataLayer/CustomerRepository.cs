using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class CustomerRepository(IDbConnectionFactory connectionFactory) : ICustomerRepository
{
    public async Task<Customer?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Customer>(
            new CommandDefinition(
                SqlObjectNames.GetProc("Customer"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<Customer> UpsertAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(customer);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", customer.Id <= 0 ? null : customer.Id, DbType.Int32, ParameterDirection.InputOutput);
        parameters.Add("@Email", customer.Email);
        parameters.Add("@FirstName", customer.FirstName);
        parameters.Add("@LastName", customer.LastName);
        parameters.Add("@CreatedUtc", customer.CreatedUtc);
        parameters.Add("@ChangedBy", customer.ChangedBy);

        return await connection.QuerySingleAsync<Customer>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("Customer"),
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }
}
