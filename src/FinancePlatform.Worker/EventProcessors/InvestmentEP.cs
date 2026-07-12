using System.Text.Json;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Investment;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Investment;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Worker.EventProcessors;

/// <summary>
/// Investment component event processor — routes codes to <see cref="IInvestmentService"/>.
/// </summary>
public sealed class InvestmentEP(IInvestmentService investmentService) : ITriggerEventProcessor
{
    public string Name => "InvestmentEP";

    public ComponentType? ComponentType => Models.Enums.ComponentType.Investment;

    public bool CanProcess(int triggerCode) =>
        TriggerCodes.IsInRange(triggerCode, Models.Enums.ComponentType.Investment);

    public Task<TriggerHandlerResult> ProcessAsync(
        TriggerContext context,
        int triggerCode,
        string payloadJson,
        ITriggerRaiser raiser,
        CancellationToken cancellationToken)
    {
        var absolute = TriggerCodes.Absolute(triggerCode);
        return Task.FromResult((absolute, TriggerCodes.IsAction(triggerCode)) switch
        {
            (TriggerCodes.InvestmentReceiveMoney, true) => EpResult.From(
                investmentService.ReceiveMoney(context, Require(payloadJson), payloadJson), raiser),
            (TriggerCodes.InvestmentInvestMoney, true) => EpResult.From(
                investmentService.InvestMoney(context, Require(payloadJson), payloadJson), raiser),
            _ => TriggerHandlerResult.Failure($"InvestmentEP does not handle trigger code {triggerCode}.")
        });
    }

    private static InvestMoneyRequest Require(string payloadJson) =>
        JsonSerializer.Deserialize<InvestMoneyRequest>(payloadJson)
        ?? throw new InvalidOperationException("Investment payload is required.");
}
