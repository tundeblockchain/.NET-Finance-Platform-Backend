namespace FinancePlatform.Services.Trading;

public interface ITradingService
{
    bool TryBuy(string idempotencyKey, Guid accountId, string assetSymbol, decimal quantity);
}
