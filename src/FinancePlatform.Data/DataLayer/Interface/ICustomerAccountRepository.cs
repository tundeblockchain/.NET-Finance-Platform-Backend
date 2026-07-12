using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface ICustomerAccountRepository
{
    Task<CustomerAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerAccount?> GetByCustomerCurrencyAsync(
        int customerId,
        string currency,
        CancellationToken cancellationToken = default);

    Task<CustomerAccount> UpsertAsync(CustomerAccount account, CancellationToken cancellationToken = default);

    Task<(CustomerAccount Account, bool AlreadyApplied)> CreditAsync(
        string idempotencyKey,
        Guid accountId,
        decimal amount,
        Guid? triggerId,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<(CustomerAccount Account, bool AlreadyApplied)> DebitAsync(
        string idempotencyKey,
        Guid accountId,
        decimal amount,
        Guid? triggerId,
        string changedBy,
        CancellationToken cancellationToken = default);
}
