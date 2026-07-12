using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class CustomerAccountRepository(IDbConnectionFactory connectionFactory) : ICustomerAccountRepository
{
    public async Task<CustomerAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<CustomerAccount>(
            new CommandDefinition(
                SqlObjectNames.GetProc("CustomerAccount"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<CustomerAccount?> GetByCustomerCurrencyAsync(
        int customerId,
        string currency,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<CustomerAccount>(
            new CommandDefinition(
                "get_CustomerAccount_ByCustomerCurrency_f",
                new { CustomerId = customerId, Currency = currency },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<CustomerAccount> UpsertAsync(CustomerAccount account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<CustomerAccount>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("CustomerAccount"),
                new
                {
                    account.Id,
                    account.CustomerId,
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

    public Task<(CustomerAccount Account, bool AlreadyApplied)> CreditAsync(
        string idempotencyKey,
        Guid accountId,
        decimal amount,
        Guid? triggerId,
        string changedBy,
        CancellationToken cancellationToken = default) =>
        MutateAsync("CreditCustomerAccount", idempotencyKey, accountId, amount, triggerId, changedBy, cancellationToken);

    public Task<(CustomerAccount Account, bool AlreadyApplied)> DebitAsync(
        string idempotencyKey,
        Guid accountId,
        decimal amount,
        Guid? triggerId,
        string changedBy,
        CancellationToken cancellationToken = default) =>
        MutateAsync("DebitCustomerAccount", idempotencyKey, accountId, amount, triggerId, changedBy, cancellationToken);

    private async Task<(CustomerAccount Account, bool AlreadyApplied)> MutateAsync(
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

        var row = await connection.QuerySingleAsync<CustomerAccountMutationRow>(
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

    private sealed class CustomerAccountMutationRow
    {
        public Guid Id { get; init; }
        public int CustomerId { get; init; }
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

        public CustomerAccount ToEntity() => new()
        {
            Id = Id,
            CustomerId = CustomerId,
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
