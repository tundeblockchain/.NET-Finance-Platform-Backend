using FinancePlatform.Data.DataLayer;
using FinancePlatform.Models;

namespace FinancePlatform.Services.Positions;

public sealed class SqlPositionService(IPositionRepository positionRepository) : IPositionService
{
    public decimal GetQuantity(Guid accountId, string assetSymbol)
    {
        var position = positionRepository
            .GetByAccountAssetAsync(accountId, assetSymbol)
            .GetAwaiter()
            .GetResult();
        return position?.Quantity ?? 0m;
    }

    public IReadOnlyList<PositionHolding> GetByAccount(Guid accountId)
    {
        var positions = positionRepository.GetByAccountAsync(accountId).GetAwaiter().GetResult();
        return positions
            .Select(p => new PositionHolding(p.AccountId, p.AssetSymbol, p.Quantity))
            .ToArray();
    }

    public bool TryApplyBuy(string idempotencyKey, Guid accountId, string assetSymbol, decimal quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetSymbol);
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        var (_, alreadyApplied) = positionRepository
            .ApplyBuyAsync(idempotencyKey, accountId, assetSymbol, quantity, ChangeActors.Broker)
            .GetAwaiter()
            .GetResult();

        return !alreadyApplied;
    }

    public PositionMutationResult TryApplySell(
        string idempotencyKey,
        Guid accountId,
        string assetSymbol,
        decimal quantity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetSymbol);
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        try
        {
            var (remaining, alreadyApplied) = positionRepository
                .ApplySellAsync(idempotencyKey, accountId, assetSymbol, quantity, ChangeActors.Broker)
                .GetAwaiter()
                .GetResult();

            return PositionMutationResult.Success(remaining, alreadyApplied);
        }
        catch (Exception ex)
        {
            return PositionMutationResult.Failure(RootMessage(ex));
        }
    }

    private static string RootMessage(Exception ex)
    {
        while (ex.InnerException is not null)
        {
            ex = ex.InnerException;
        }

        return ex.Message;
    }
}
