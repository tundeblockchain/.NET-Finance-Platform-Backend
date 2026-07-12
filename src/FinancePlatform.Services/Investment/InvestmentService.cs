using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Investment;
using FinancePlatform.Models.Triggers;

namespace FinancePlatform.Services.Investment;

/// <summary>
/// Main investment component service.
/// </summary>
public sealed class InvestmentService : IInvestmentService
{
    public ComponentOperationResult ReceiveMoney(
        TriggerContext context,
        InvestMoneyRequest request,
        string rawPayloadJson)
    {
        _ = request;
        return ComponentOperationResult.Success(
            resultJson: """{"status":"investment-received"}""",
            nextTriggers:
            [
                new NextTriggerSpec
                {
                    TriggerCode = TriggerCodes.InvestmentInvestMoney,
                    QueueName = QueueNames.Investment,
                    TargetComponent = "Investment",
                    PayloadJson = rawPayloadJson,
                    IdempotencyKey = $"{context.IdempotencyKey}:8002"
                }
            ]);
    }

    public ComponentOperationResult InvestMoney(
        TriggerContext context,
        InvestMoneyRequest request,
        string rawPayloadJson)
    {
        _ = request;
        return ComponentOperationResult.Success(
            resultJson: """{"status":"investment-invested"}""",
            nextTriggers:
            [
                new NextTriggerSpec
                {
                    TriggerCode = TriggerCodes.AssetBuyAsset,
                    QueueName = QueueNames.AssetTrading,
                    TargetComponent = "AssetTrading",
                    PayloadJson = rawPayloadJson,
                    IdempotencyKey = $"{context.IdempotencyKey}:9001"
                }
            ]);
    }
}
