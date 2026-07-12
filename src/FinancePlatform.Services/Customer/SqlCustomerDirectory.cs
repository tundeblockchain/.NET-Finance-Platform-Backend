using FinancePlatform.Data.DataLayer;
using FinancePlatform.Models;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Entities;
using CustomerEntity = FinancePlatform.Models.Entities.Customer;

namespace FinancePlatform.Services.Customer;

/// <summary>
/// SQL-backed customer directory. All fetch/update paths go through stored procedures.
/// </summary>
public sealed class SqlCustomerDirectory(
    ICustomerProvisionRepository provisionRepository,
    ICustomerRepository customerRepository,
    ICustomerAddressRepository addressRepository,
    ICustomerAccountRepository customerAccountRepository,
    ITradingAccountRepository tradingAccountRepository,
    IDistributionAgreementRepository agreementRepository,
    IDistributionElementRepository elementRepository) : ICustomerDirectory
{
    public CustomerProvisioningResult CreateCustomer(CreateCustomerRequest request)
    {
        var bundle = provisionRepository.ProvisionAsync(request).GetAwaiter().GetResult();
        return new CustomerProvisioningResult
        {
            Customer = bundle.Customer,
            Address = bundle.Address,
            CustomerAccount = bundle.CustomerAccount,
            TradingAccount = bundle.TradingAccount,
            DistributionAgreement = bundle.DistributionAgreement
        };
    }

    public CustomerEntity? FindCustomer(int customerId) =>
        customerRepository.GetAsync(customerId).GetAwaiter().GetResult();

    public CustomerAddress? FindAddress(int customerId) =>
        addressRepository.GetByCustomerIdAsync(customerId).GetAwaiter().GetResult();

    public DistributionAgreement? FindAgreementByOwnerAccount(Guid ownerAccountId) =>
        agreementRepository.GetByOwnerAccountAsync(ownerAccountId).GetAwaiter().GetResult();

    public CustomerAccount? FindCustomerAccount(Guid customerAccountId) =>
        customerAccountRepository.GetAsync(customerAccountId).GetAwaiter().GetResult();

    public CustomerAccount? FindCustomerAccountByCustomer(int customerId, string currency) =>
        customerAccountRepository
            .GetByCustomerCurrencyAsync(customerId, currency.ToUpperInvariant())
            .GetAwaiter()
            .GetResult();

    public TradingAccount? FindTradingAccount(Guid tradingAccountId) =>
        tradingAccountRepository.GetAsync(tradingAccountId).GetAwaiter().GetResult();

    public TradingAccount? FindTradingAccountByCustomer(int customerId, string currency) =>
        tradingAccountRepository
            .GetByCustomerCurrencyAsync(customerId, currency.ToUpperInvariant())
            .GetAwaiter()
            .GetResult();

    public IReadOnlyList<DistributionElement> GetActiveElements(Guid ownerAccountId)
    {
        var agreement = FindAgreementByOwnerAccount(ownerAccountId);
        if (agreement is null)
        {
            return [];
        }

        return elementRepository.GetByAgreementIdAsync(agreement.Id).GetAwaiter().GetResult();
    }

    public bool TryCreditCustomerAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey)
    {
        try
        {
            customerAccountRepository
                .CreditAsync(idempotencyKey, accountId, amount, triggerId, ChangeActors.Broker)
                .GetAwaiter()
                .GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryDebitCustomerAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey)
    {
        try
        {
            customerAccountRepository
                .DebitAsync(idempotencyKey, accountId, amount, triggerId, ChangeActors.Broker)
                .GetAwaiter()
                .GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryCreditTradingAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey)
    {
        try
        {
            tradingAccountRepository
                .CreditAsync(idempotencyKey, accountId, amount, triggerId, ChangeActors.Broker)
                .GetAwaiter()
                .GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryDebitTradingAccount(Guid accountId, decimal amount, Guid triggerId, string idempotencyKey)
    {
        try
        {
            tradingAccountRepository
                .DebitAsync(idempotencyKey, accountId, amount, triggerId, ChangeActors.Broker)
                .GetAwaiter()
                .GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public decimal GetCustomerSettled(Guid accountId) =>
        FindCustomerAccount(accountId)?.Settled ?? 0m;

    public decimal GetTradingSettled(Guid accountId) =>
        FindTradingAccount(accountId)?.Settled ?? 0m;
}
