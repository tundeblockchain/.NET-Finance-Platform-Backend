using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface IInvestmentAccountRepository
{
    Task<InvestmentAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvestmentAccount?> GetByTradingAccountAsync(
        Guid tradingAccountId,
        CancellationToken cancellationToken = default);

    Task<InvestmentAccount> EnsureAsync(
        int customerId,
        Guid tradingAccountId,
        string currency,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<InvestmentAccount> UpsertAsync(InvestmentAccount account, CancellationToken cancellationToken = default);

    Task<(InvestmentAccount Account, bool AlreadyApplied)> CreditAsync(
        string idempotencyKey,
        Guid accountId,
        decimal amount,
        Guid? triggerId,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<(InvestmentAccount Account, bool AlreadyApplied)> DebitAsync(
        string idempotencyKey,
        Guid accountId,
        decimal amount,
        Guid? triggerId,
        string changedBy,
        CancellationToken cancellationToken = default);
}
