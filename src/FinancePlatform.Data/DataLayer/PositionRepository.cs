using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class PositionRepository(IDbConnectionFactory connectionFactory) : IPositionRepository
{
    public async Task<Position?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Position>(
            new CommandDefinition(
                SqlObjectNames.GetProc("Position"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<Position> UpsertAsync(Position entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleAsync<Position>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("Position"),
                new
                {
                    entity.Id,
                    entity.AccountId,
                    entity.AssetSymbol,
                    entity.Quantity,
                    entity.AverageCost,
                    entity.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }
}
