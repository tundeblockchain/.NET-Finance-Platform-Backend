using FinancePlatform.Api.Contracts;
using FinancePlatform.Services.Customer;

namespace FinancePlatform.Api.Contracts;

internal static class CustomerMapper
{
    public static CustomerResponse ToResponse(CustomerProvisioningResult result) => new(
        result.Customer.Id,
        result.Customer.Email,
        result.Customer.FirstName,
        result.Customer.LastName,
        result.Address is null
            ? null
            : new AddressResponse(
                result.Address.Line1,
                result.Address.Line2,
                result.Address.City,
                result.Address.Region,
                result.Address.PostalCode,
                result.Address.Country),
        ToCustomerAccountBalance(result.CustomerAccount),
        ToTradingAccountBalance(result.TradingAccount),
        result.DistributionAgreement.Id);

    public static AccountBalanceResponse ToCustomerAccountBalance(
        FinancePlatform.Models.Entities.CustomerAccount account) =>
        new(account.Id, account.CustomerId, account.Currency, account.Settled, account.Reserved, account.Available);

    public static AccountBalanceResponse ToTradingAccountBalance(
        FinancePlatform.Models.Entities.TradingAccount account) =>
        new(account.Id, account.CustomerId, account.Currency, account.Settled, account.Reserved, account.Available);
}
