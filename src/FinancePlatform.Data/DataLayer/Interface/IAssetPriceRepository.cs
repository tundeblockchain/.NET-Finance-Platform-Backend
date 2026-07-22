using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface IAssetPriceRepository
{
    Task<AssetPrice?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AssetPrice?> GetLatestAsync(string assetSymbol, CancellationToken cancellationToken = default);

    Task<AssetPrice> UpsertAsync(AssetPrice entity, CancellationToken cancellationToken = default);
}
