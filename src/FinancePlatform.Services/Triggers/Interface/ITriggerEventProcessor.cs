using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Triggers;

/// <summary>
/// Component event processor: routes owned trigger codes via an internal switch and calls services.
/// </summary>
public interface ITriggerEventProcessor
{
    string Name { get; }

    ComponentType? ComponentType { get; }

    bool CanProcess(int triggerCode);

    Task<TriggerHandlerResult> ProcessAsync(
        TriggerContext context,
        int triggerCode,
        string payloadJson,
        ITriggerRaiser raiser,
        CancellationToken cancellationToken);
}
