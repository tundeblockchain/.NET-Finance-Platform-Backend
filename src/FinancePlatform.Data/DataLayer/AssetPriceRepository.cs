using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public sealed class AssetPriceRepository(IDbConnectionFactory connectionFactory) : IAssetPriceRepository
{
    public async Task<AssetPrice?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AssetPrice>(
            new CommandDefinition(
                SqlObjectNames.GetProc("AssetPrice"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<AssetPrice?> GetLatestAsync(string assetSymbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetSymbol);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AssetPrice>(
            new CommandDefinition(
                "get_AssetPrice_latest_f",
                new { AssetSymbol = assetSymbol.Trim().ToUpperInvariant() },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<AssetPrice> UpsertAsync(AssetPrice entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleAsync<AssetPrice>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("AssetPrice"),
                new
                {
                    entity.Id,
                    entity.AssetSymbol,
                    entity.Price,
                    entity.Currency,
                    Source = (int)entity.Source,
                    entity.Provider,
                    entity.OrderId,
                    entity.ExternalOrderId,
                    entity.ObservedUtc,
                    entity.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }
}
