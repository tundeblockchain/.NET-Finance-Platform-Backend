using System.Text.Json;
using FinancePlatform.Models.Cash;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Worker.EventProcessors;

/// <summary>
/// Cash component event processor — routes codes to <see cref="ICashComponentService"/>.
/// </summary>
public sealed class CashEP(ICashComponentService cashComponentService) : ITriggerEventProcessor
{
    public string Name => "CashEP";

    public ComponentType? ComponentType => null;

    public bool CanProcess(int triggerCode) =>
        TriggerCodes.Absolute(triggerCode) == TriggerCodes.DepositCash;

    public Task<TriggerHandlerResult> ProcessAsync(
        TriggerContext context,
        int triggerCode,
        string payloadJson,
        ITriggerRaiser raiser,
        CancellationToken cancellationToken)
    {
        var code = TriggerCodes.Absolute(triggerCode);
        return Task.FromResult(code switch
        {
            TriggerCodes.DepositCash when TriggerCodes.IsAction(triggerCode) => EpResult.From(
                cashComponentService.Deposit(
                    context,
                    JsonSerializer.Deserialize<DepositCashRequest>(payloadJson)
                    ?? throw new InvalidOperationException("Deposit payload is required.")),
                raiser),
            _ => TriggerHandlerResult.Failure($"CashEP does not handle trigger code {triggerCode}.")
        });
    }
}
