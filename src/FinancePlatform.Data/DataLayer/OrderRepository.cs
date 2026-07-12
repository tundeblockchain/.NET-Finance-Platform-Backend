using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class OrderRepository(IDbConnectionFactory connectionFactory) : IOrderRepository
{
    public async Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Order>(
            new CommandDefinition(
                SqlObjectNames.GetProc("Order"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<Order> UpsertAsync(Order entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleAsync<Order>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("Order"),
                new
                {
                    entity.Id,
                    entity.AccountId,
                    entity.AllocationRequestId,
                    entity.TriggerId,
                    entity.AssetSymbol,
                    Side = (int)entity.Side,
                    entity.Quantity,
                    entity.LimitPrice,
                    Status = (int)entity.Status,
                    entity.IdempotencyKey,
                    entity.CreatedUtc,
                    entity.SubmittedUtc,
                    entity.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }
}
