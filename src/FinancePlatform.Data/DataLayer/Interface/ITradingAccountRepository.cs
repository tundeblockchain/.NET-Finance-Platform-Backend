using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface ITradingAccountRepository
{
    Task<TradingAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TradingAccount?> GetByCustomerCurrencyAsync(
        int customerId,
        string currency,
        CancellationToken cancellationToken = default);

    Task<TradingAccount> UpsertAsync(TradingAccount account, CancellationToken cancellationToken = default);

    Task<(TradingAccount Account, bool AlreadyApplied)> CreditAsync(
        string idempotencyKey,
        Guid accountId,
        decimal amount,
        Guid? triggerId,
        string changedBy,
        CancellationToken cancellationToken = default);
}
