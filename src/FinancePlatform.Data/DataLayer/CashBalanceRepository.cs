using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class CashBalanceRepository(IDbConnectionFactory connectionFactory) : ICashBalanceRepository
{
    public async Task<CashBalance?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CashBalance>(
            new CommandDefinition(
                SqlObjectNames.GetProc("CashBalance"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<CashBalance> UpsertAsync(CashBalance entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleAsync<CashBalance>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("CashBalance"),
                new
                {
                    entity.Id,
                    entity.AccountId,
                    entity.Currency,
                    entity.Settled,
                    entity.Reserved,
                    entity.IsLocked,
                    entity.LockedByAllocationId,
                    entity.LockedByTriggerId,
                    entity.LockAcquiredUtc,
                    entity.LockExpiresUtc,
                    entity.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }
}
