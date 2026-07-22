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

    public async Task<Position?> GetByAccountAssetAsync(
        Guid accountId,
        string assetSymbol,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Position>(
            new CommandDefinition(
                "get_Position_ByAccountAsset_f",
                new { AccountId = accountId, AssetSymbol = assetSymbol },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Position>> GetByAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync<Position>(
            new CommandDefinition(
                "get_Position_ByAccountId_f",
                new { AccountId = accountId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<(Position? Position, bool AlreadyApplied)> ApplyBuyAsync(
        string idempotencyKey,
        Guid accountId,
        string assetSymbol,
        decimal quantity,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<PositionWithFlag>(
            new CommandDefinition(
                "ApplyPositionBuy",
                new
                {
                    IdempotencyKey = idempotencyKey,
                    AccountId = accountId,
                    AssetSymbol = assetSymbol,
                    Quantity = quantity,
                    ChangedBy = changedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return (row?.ToPosition(), row?.AlreadyApplied ?? true);
    }

    public async Task<(decimal Quantity, bool AlreadyApplied)> ApplySellAsync(
        string idempotencyKey,
        Guid accountId,
        string assetSymbol,
        decimal quantity,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleAsync<SellResultRow>(
            new CommandDefinition(
                "ApplyPositionSell",
                new
                {
                    IdempotencyKey = idempotencyKey,
                    AccountId = accountId,
                    AssetSymbol = assetSymbol,
                    Quantity = quantity,
                    ChangedBy = changedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return (row.Quantity, row.AlreadyApplied);
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

    private sealed class PositionWithFlag
    {
        public Guid Id { get; init; }
        public Guid AccountId { get; init; }
        public string AssetSymbol { get; init; } = "";
        public decimal Quantity { get; init; }
        public decimal AverageCost { get; init; }
        public DateTimeOffset DateModified { get; init; }
        public string ChangedBy { get; init; } = "";
        public bool AlreadyApplied { get; init; }

        public Position ToPosition() => new()
        {
            Id = Id,
            AccountId = AccountId,
            AssetSymbol = AssetSymbol,
            Quantity = Quantity,
            AverageCost = AverageCost,
            DateModified = DateModified,
            ChangedBy = ChangedBy
        };
    }

    private sealed class SellResultRow
    {
        public decimal Quantity { get; init; }
        public bool AlreadyApplied { get; init; }
    }
}
