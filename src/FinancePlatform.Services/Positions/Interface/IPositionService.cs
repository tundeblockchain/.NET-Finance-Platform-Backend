namespace FinancePlatform.Services.Positions;

public interface IPositionService
{
    decimal GetQuantity(Guid accountId, string assetSymbol);

    IReadOnlyList<PositionHolding> GetByAccount(Guid accountId);

    /// <summary>
    /// Increases position. Idempotent by key. Returns false when already applied.
    /// </summary>
    bool TryApplyBuy(string idempotencyKey, Guid accountId, string assetSymbol, decimal quantity);

    /// <summary>
    /// Decreases position when sufficient quantity exists. Idempotent by key.
    /// </summary>
    PositionMutationResult TryApplySell(string idempotencyKey, Guid accountId, string assetSymbol, decimal quantity);
}
