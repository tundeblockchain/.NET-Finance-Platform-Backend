using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

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
}
