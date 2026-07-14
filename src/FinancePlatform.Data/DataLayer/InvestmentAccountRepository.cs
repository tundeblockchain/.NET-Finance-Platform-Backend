using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class InvestmentAccountRepository(IDbConnectionFactory connectionFactory) : IInvestmentAccountRepository
{
    public async Task<InvestmentAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<InvestmentAccount>(
            new CommandDefinition(
                SqlObjectNames.GetProc("InvestmentAccount"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<InvestmentAccount?> GetByTradingAccountAsync(
        Guid tradingAccountId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<InvestmentAccount>(
            new CommandDefinition(
                "get_InvestmentAccount_ByTradingAccount_f",
                new { TradingAccountId = tradingAccountId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<InvestmentAccount> EnsureAsync(
        int customerId,
        Guid tradingAccountId,
        string currency,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<InvestmentAccount>(
            new CommandDefinition(
                "EnsureInvestmentAccount",
                new
                {
                    CustomerId = customerId,
                    TradingAccountId = tradingAccountId,
                    Currency = currency,
                    ChangedBy = changedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<InvestmentAccount> UpsertAsync(InvestmentAccount account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<InvestmentAccount>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("InvestmentAccount"),
                new
                {
                    account.Id,
                    account.CustomerId,
                    account.TradingAccountId,
                    account.Currency,
                    account.Settled,
                    account.Reserved,
                    account.IsLocked,
                    account.LockedByTriggerId,
                    account.LockExpiresUtc,
                    account.CreatedUtc,
                    account.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public Task<(InvestmentAccount Account, bool AlreadyApplied)> CreditAsync(
        string idempotencyKey,
        Guid accountId,
        decimal amount,
        Guid? triggerId,
        string changedBy,
        CancellationToken cancellationToken = default) =>
        MutateAsync("CreditInvestmentAccount", idempotencyKey, accountId, amount, triggerId, changedBy, cancellationToken);

    public Task<(InvestmentAccount Account, bool AlreadyApplied)> DebitAsync(
        string idempotencyKey,
        Guid accountId,
        decimal amount,
        Guid? triggerId,
        string changedBy,
        CancellationToken cancellationToken = default) =>
        MutateAsync("DebitInvestmentAccount", idempotencyKey, accountId, amount, triggerId, changedBy, cancellationToken);

    private async Task<(InvestmentAccount Account, bool AlreadyApplied)> MutateAsync(
        string procedure,
        string idempotencyKey,
        Guid accountId,
        decimal amount,
        Guid? triggerId,
        string changedBy,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleAsync<InvestmentAccountMutationRow>(
            new CommandDefinition(
                procedure,
                new
                {
                    IdempotencyKey = idempotencyKey,
                    AccountId = accountId,
                    Amount = amount,
                    TriggerId = triggerId,
                    ChangedBy = changedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return (row.ToEntity(), row.AlreadyApplied);
    }

    private sealed class InvestmentAccountMutationRow
    {
        public Guid Id { get; init; }
        public int CustomerId { get; init; }
        public Guid TradingAccountId { get; init; }
        public string Currency { get; init; } = "";
        public decimal Settled { get; init; }
        public decimal Reserved { get; init; }
        public bool IsLocked { get; init; }
        public Guid? LockedByTriggerId { get; init; }
        public DateTimeOffset? LockExpiresUtc { get; init; }
        public DateTimeOffset CreatedUtc { get; init; }
        public DateTimeOffset DateModified { get; init; }
        public string ChangedBy { get; init; } = "";
        public bool AlreadyApplied { get; init; }

        public InvestmentAccount ToEntity() => new()
        {
            Id = Id,
            CustomerId = CustomerId,
            TradingAccountId = TradingAccountId,
            Currency = Currency,
            Settled = Settled,
            Reserved = Reserved,
            IsLocked = IsLocked,
            LockedByTriggerId = LockedByTriggerId,
            LockExpiresUtc = LockExpiresUtc,
            CreatedUtc = CreatedUtc,
            DateModified = DateModified,
            ChangedBy = ChangedBy
        };
    }
}
