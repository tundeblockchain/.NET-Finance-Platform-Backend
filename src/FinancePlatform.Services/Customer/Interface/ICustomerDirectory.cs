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

    InvestmentAccount EnsureInvestmentAccount(int customerId, Guid tradingAccountId, string currency);

    InvestmentAccount? FindInvestmentAccount(Guid investmentAccountId);

    InvestmentAccount? FindInvestmentAccountByTradingAccount(Guid tradingAccountId);

    DistributionAgreement EnsureTradingToInvestmentDistribution(
        int customerId,
        Guid tradingAccountId,
        Guid investmentAccountId);

    CustomerAccount? FindCustomerAccount(Guid customerAccountId);

    CustomerAccount? FindCustomerAccountByCustomer(int customerId, string currency);

    TradingAccount? FindTradingAccount(Guid tradingAccountId);

    TradingAccount? FindTradingAccountByCustomer(int customerId, string currency);

    IReadOnlyList<DistributionElement> GetActiveElements(Guid ownerAccountId);

    bool TryCreditCustomerAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey);

    bool TryDebitCustomerAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey);

    bool TryCreditTradingAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey);

    bool TryDebitTradingAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey);

    bool TryCreditInvestmentAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey);

    bool TryDebitInvestmentAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey);

    decimal GetCustomerSettled(Guid accountId);

    decimal GetTradingSettled(Guid accountId);

    decimal GetTradingAvailable(Guid tradingAccountId, decimal pendingInstructionCash);

    decimal GetInvestmentSettled(Guid investmentAccountId);
}
