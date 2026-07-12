using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class CustomerAddressRepository(IDbConnectionFactory connectionFactory) : ICustomerAddressRepository
{
    public async Task<CustomerAddress?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<CustomerAddress>(
            new CommandDefinition(
                SqlObjectNames.GetProc("CustomerAddress"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<CustomerAddress?> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<CustomerAddress>(
            new CommandDefinition(
                "get_CustomerAddress_ByCustomerId_f",
                new { CustomerId = customerId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<CustomerAddress> UpsertAsync(CustomerAddress address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<CustomerAddress>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("CustomerAddress"),
                new
                {
                    address.Id,
                    address.CustomerId,
                    address.Line1,
                    address.Line2,
                    address.City,
                    address.Region,
                    address.PostalCode,
                    address.Country,
                    address.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }
}
