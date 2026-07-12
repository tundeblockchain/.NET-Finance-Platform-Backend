using FinancePlatform.Models.Asset;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Services.Allocation;
using FinancePlatform.Services.Trade;

namespace FinancePlatform.Services.Asset;

public interface IAssetService
{
    ComponentOperationResult Buy(TriggerContext context, AssetOrderRequest request);

    ComponentOperationResult Sell(TriggerContext context, AssetOrderRequest request);

    ComponentOperationResult ReverseBuy(TriggerContext context, AssetOrderRequest request);
}
