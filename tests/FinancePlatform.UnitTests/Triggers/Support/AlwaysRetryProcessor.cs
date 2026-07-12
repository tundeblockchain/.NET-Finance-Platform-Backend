using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.UnitTests.Triggers.Support;

internal sealed class AlwaysRetryProcessor(int triggerCode) : ITriggerEventProcessor
{
    public string Name => "AlwaysRetryProcessor";

    public ComponentType? ComponentType => null;

    public bool CanProcess(int code) => Math.Abs(code) == Math.Abs(triggerCode);

    public Task<TriggerHandlerResult> ProcessAsync(
        TriggerContext context,
        int code,
        string payloadJson,
        ITriggerRaiser raiser,
        CancellationToken cancellationToken) =>
        Task.FromResult(TriggerHandlerResult.Retry("busy"));
}
