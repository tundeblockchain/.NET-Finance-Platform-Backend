using System.Text.Json;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Worker.EventProcessors;

/// <summary>
/// Customer component event processor — routes codes to <see cref="ICustomerService"/>.
/// </summary>
public sealed class CustomerEP(ICustomerService customerService) : ITriggerEventProcessor
{
    public string Name => "CustomerEP";

    public ComponentType? ComponentType => Models.Enums.ComponentType.Customer;

    public bool CanProcess(int triggerCode) =>
        TriggerCodes.IsInRange(triggerCode, Models.Enums.ComponentType.Customer);

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
            TriggerCodes.CustomerDepositMoney when TriggerCodes.IsAction(triggerCode) => EpResult.From(
                customerService.DepositMoney(
                    context,
                    JsonSerializer.Deserialize<CustomerDepositRequest>(payloadJson)
                    ?? throw new InvalidOperationException("Customer deposit payload is required.")),
                raiser),
            TriggerCodes.CustomerReceiveMoney when TriggerCodes.IsAction(triggerCode) => EpResult.From(
                customerService.ReceiveMoney(
                    context,
                    JsonSerializer.Deserialize<CustomerReceiveMoneyRequest>(payloadJson)
                    ?? throw new InvalidOperationException("Customer receive payload is required.")),
                raiser),
            TriggerCodes.CustomerDistributeMoney when TriggerCodes.IsAction(triggerCode) => EpResult.From(
                customerService.DistributeMoney(
                    context,
                    JsonSerializer.Deserialize<DistributeMoneyRequest>(payloadJson)
                    ?? throw new InvalidOperationException("Customer distribute payload is required."),
                    payloadJson),
                raiser),
            _ => TriggerHandlerResult.Failure($"CustomerEP does not handle trigger code {triggerCode}.")
        });
    }
}
