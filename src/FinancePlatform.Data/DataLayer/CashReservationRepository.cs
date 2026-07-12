using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class CashReservationRepository(IDbConnectionFactory connectionFactory) : ICashReservationRepository
{
    public async Task<CashReservation?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CashReservation>(
            new CommandDefinition(
                SqlObjectNames.GetProc("CashReservation"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<CashReservation> UpsertAsync(CashReservation entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleAsync<CashReservation>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("CashReservation"),
                new
                {
                    entity.Id,
                    entity.AccountId,
                    entity.AllocationRequestId,
                    entity.TriggerId,
                    entity.Currency,
                    entity.Amount,
                    entity.IdempotencyKey,
                    entity.IsReleased,
                    entity.CreatedUtc,
                    entity.ReleasedUtc,
                    entity.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }
}
