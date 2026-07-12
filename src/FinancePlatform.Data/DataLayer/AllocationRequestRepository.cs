using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class AllocationRequestRepository(IDbConnectionFactory connectionFactory) : IAllocationRequestRepository
{
    public async Task<AllocationRequest?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AllocationRequest>(
            new CommandDefinition(
                SqlObjectNames.GetProc("AllocationRequest"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<AllocationRequest> UpsertAsync(AllocationRequest entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleAsync<AllocationRequest>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("AllocationRequest"),
                new
                {
                    entity.Id,
                    entity.CustomerId,
                    entity.AccountId,
                    entity.IdempotencyKey,
                    Status = (int)entity.Status,
                    entity.Amount,
                    entity.Currency,
                    entity.RootWorkflowId,
                    entity.CreatedUtc,
                    entity.CompletedUtc,
                    entity.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }
}
