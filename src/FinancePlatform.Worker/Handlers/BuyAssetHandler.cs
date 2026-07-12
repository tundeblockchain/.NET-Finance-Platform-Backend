using System.Text.Json;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Trading;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Worker.Handlers;

public sealed class BuyAssetHandler(ITradingService tradingService) : ITriggerHandler
{
    public int TriggerCode => TriggerCodes.BuyAsset;

    public Task<TriggerHandlerResult> ExecuteAsync(
        TriggerContext context,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<BuyAssetPayload>(payloadJson)
            ?? throw new InvalidOperationException("Buy payload is required.");

        var accountId = context.ExternalId
            ?? throw new InvalidOperationException("Buy requires ExternalId (Account).");

        tradingService.TryBuy(context.IdempotencyKey, accountId, payload.AssetSymbol, payload.Quantity);

        return Task.FromResult(TriggerHandlerResult.Success(resultJson: """{"status":"bought"}"""));
    }
}
