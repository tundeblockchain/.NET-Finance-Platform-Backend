using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class CustomerProvisionRepository(IDbConnectionFactory connectionFactory)
    : ICustomerProvisionRepository
{
    public async Task<ProvisionedCustomerBundle> ProvisionAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currency = string.IsNullOrWhiteSpace(request.Currency)
            ? "GBP"
            : request.Currency.ToUpperInvariant();

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                "ProvisionCustomer",
                new
                {
                    request.Email,
                    request.FirstName,
                    request.LastName,
                    Currency = currency,
                    Line1 = request.Address?.Line1,
                    Line2 = request.Address?.Line2,
                    City = request.Address?.City,
                    Region = request.Address?.Region,
                    PostalCode = request.Address?.PostalCode,
                    Country = request.Address?.Country,
                    ChangedBy = ChangeActors.System
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var customer = await multi.ReadSingleAsync<Customer>();
        var address = (await multi.ReadAsync<CustomerAddress>()).FirstOrDefault();
        var customerAccount = await multi.ReadSingleAsync<CustomerAccount>();
        var tradingAccount = await multi.ReadSingleAsync<TradingAccount>();
        var agreement = await multi.ReadSingleAsync<DistributionAgreement>();

        return new ProvisionedCustomerBundle
        {
            Customer = customer,
            Address = address,
            CustomerAccount = customerAccount,
            TradingAccount = tradingAccount,
            DistributionAgreement = agreement
        };
    }
}
