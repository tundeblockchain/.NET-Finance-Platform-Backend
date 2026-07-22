using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface IPositionRepository
{
    Task<Position?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Position?> GetByAccountAssetAsync(
        Guid accountId,
        string assetSymbol,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Position>> GetByAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<(Position? Position, bool AlreadyApplied)> ApplyBuyAsync(
        string idempotencyKey,
        Guid accountId,
        string assetSymbol,
        decimal quantity,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<(decimal Quantity, bool AlreadyApplied)> ApplySellAsync(
        string idempotencyKey,
        Guid accountId,
        string assetSymbol,
        decimal quantity,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<Position> UpsertAsync(Position entity, CancellationToken cancellationToken = default);
}
