using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Data.DataLayer;

public sealed class LedgerEntryRepository(IDbConnectionFactory connectionFactory) : ILedgerEntryRepository
{
    public async Task<LedgerEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<LedgerEntry>(
            new CommandDefinition(
                SqlObjectNames.GetProc("LedgerEntry"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<LedgerEntry?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<LedgerEntry>(
            new CommandDefinition(
                "get_LedgerEntry_ByIdempotencyKey_f",
                new { IdempotencyKey = idempotencyKey },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<(LedgerEntry Entry, bool AlreadyApplied)> CreateAsync(
        Guid id,
        Guid accountId,
        Guid? triggerId,
        Guid? allocationRequestId,
        LedgerEntryType entryType,
        decimal amount,
        string currency,
        string idempotencyKey,
        string description,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleAsync<LedgerEntryWithFlag>(
            new CommandDefinition(
                CashProcedureNames.CreateLedgerEntry,
                new
                {
                    Id = id,
                    AccountId = accountId,
                    TriggerId = triggerId,
                    AllocationRequestId = allocationRequestId,
                    EntryType = (int)entryType,
                    Amount = amount,
                    Currency = currency,
                    IdempotencyKey = idempotencyKey,
                    Description = description,
                    ChangedBy = changedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return (row.ToEntry(), row.AlreadyApplied);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                "get_LedgerEntry_Count_f",
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<LedgerEntry> UpsertAsync(LedgerEntry entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleAsync<LedgerEntry>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("LedgerEntry"),
                new
                {
                    entity.Id,
                    entity.AccountId,
                    entity.TriggerId,
                    entity.AllocationRequestId,
                    EntryType = (int)entity.EntryType,
                    entity.Amount,
                    entity.Currency,
                    entity.IdempotencyKey,
                    entity.Description,
                    entity.PostedUtc,
                    entity.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    private sealed class LedgerEntryWithFlag
    {
        public Guid Id { get; init; }
        public Guid AccountId { get; init; }
        public Guid? TriggerId { get; init; }
        public Guid? AllocationRequestId { get; init; }
        public int EntryType { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "";
        public string IdempotencyKey { get; init; } = "";
        public string Description { get; init; } = "";
        public DateTimeOffset PostedUtc { get; init; }
        public DateTimeOffset DateModified { get; init; }
        public string ChangedBy { get; init; } = "";
        public bool AlreadyApplied { get; init; }

        public LedgerEntry ToEntry() => new()
        {
            Id = Id,
            AccountId = AccountId,
            TriggerId = TriggerId,
            AllocationRequestId = AllocationRequestId,
            EntryType = (LedgerEntryType)EntryType,
            Amount = Amount,
            Currency = Currency,
            IdempotencyKey = IdempotencyKey,
            Description = Description,
            PostedUtc = PostedUtc,
            DateModified = DateModified,
            ChangedBy = ChangedBy
        };
    }
}
