using System.Text.Json;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Worker.Handlers;

public sealed class DepositCashHandler(ICashService cashService) : ITriggerHandler
{
    public int TriggerCode => TriggerCodes.DepositCash;

    public Task<TriggerHandlerResult> ExecuteAsync(
        TriggerContext context,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<DepositCashPayload>(payloadJson)
            ?? throw new InvalidOperationException("Deposit payload is required.");

        var accountId = context.ExternalId
            ?? throw new InvalidOperationException("Deposit requires ExternalId (Account).");

        cashService.TryDeposit(context.IdempotencyKey, accountId, payload.Amount, payload.Currency);

        var next = new NextTriggerRequest
        {
            TriggerCode = TriggerCodes.BuyAsset,
            QueueName = "Trading",
            TargetComponent = "Trading",
            PayloadJson = JsonSerializer.Serialize(new BuyAssetPayload
            {
                AssetSymbol = payload.AssetSymbol,
                Quantity = payload.Quantity
            }),
            IdempotencyKey = $"{context.IdempotencyKey}:buy"
        };

        return Task.FromResult(TriggerHandlerResult.Success(
            resultJson: """{"status":"deposited"}""",
            nextTriggers: [next]));
    }
}
