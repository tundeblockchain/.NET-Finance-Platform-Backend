using System.Text.Json;
using FinancePlatform.Models.Asset;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Asset;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Worker.EventProcessors;

/// <summary>
/// Asset trading component event processor — routes codes to <see cref="IAssetService"/>.
/// </summary>
public sealed class AssetEP(IAssetService assetService) : ITriggerEventProcessor
{
    public string Name => "AssetEP";

    public ComponentType? ComponentType => Models.Enums.ComponentType.AssetTrading;

    public bool CanProcess(int triggerCode) =>
        TriggerCodes.IsInRange(triggerCode, Models.Enums.ComponentType.AssetTrading);

    public async Task<TriggerHandlerResult> ProcessAsync(
        TriggerContext context,
        int triggerCode,
        string payloadJson,
        ITriggerRaiser raiser,
        CancellationToken cancellationToken)
    {
        var absolute = TriggerCodes.Absolute(triggerCode);
        return (absolute, TriggerCodes.IsAction(triggerCode)) switch
        {
            (TriggerCodes.AssetBuyAsset, true) => EpResult.From(
                await assetService.BuyAsync(context, Require(payloadJson), cancellationToken), raiser),
            (TriggerCodes.AssetBuyAsset, false) => EpResult.From(
                await assetService.ReverseBuyAsync(context, Require(payloadJson), cancellationToken), raiser),
            (TriggerCodes.AssetSellAsset, true) => EpResult.From(
                await assetService.SellAsync(context, Require(payloadJson), cancellationToken), raiser),
            _ => TriggerHandlerResult.Failure($"AssetEP does not handle trigger code {triggerCode}.")
        };
    }

    private static AssetOrderRequest Require(string payloadJson) =>
        JsonSerializer.Deserialize<AssetOrderRequest>(payloadJson)
        ?? throw new InvalidOperationException("Asset payload is required.");
}
