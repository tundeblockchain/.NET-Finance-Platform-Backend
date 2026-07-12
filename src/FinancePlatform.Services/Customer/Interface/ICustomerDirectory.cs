using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Entities;
using CustomerEntity = FinancePlatform.Models.Entities.Customer;

namespace FinancePlatform.Services.Customer;

public interface ICustomerDirectory
{
    CustomerProvisioningResult CreateCustomer(CreateCustomerRequest request);

    CustomerEntity? FindCustomer(int customerId);

    CustomerAddress? FindAddress(int customerId);

    DistributionAgreement? FindAgreementByOwnerAccount(Guid ownerAccountId);

    CustomerAccount? FindCustomerAccount(Guid customerAccountId);

    CustomerAccount? FindCustomerAccountByCustomer(int customerId, string currency);

    TradingAccount? FindTradingAccount(Guid tradingAccountId);

    TradingAccount? FindTradingAccountByCustomer(int customerId, string currency);

    IReadOnlyList<DistributionElement> GetActiveElements(Guid ownerAccountId);

    bool TryCreditCustomerAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey);

    bool TryDebitCustomerAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey);

    bool TryCreditTradingAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey);

    decimal GetCustomerSettled(Guid accountId);

    decimal GetTradingSettled(Guid accountId);
}
