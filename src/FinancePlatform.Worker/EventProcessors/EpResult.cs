using FinancePlatform.Models.Components;
using FinancePlatform.Models.Enums;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Worker.EventProcessors;

/// <summary>
/// Applies service <see cref="NextTriggerSpec"/> values via <see cref="ITriggerRaiser"/> and maps to engine results.
/// </summary>
internal static class EpResult
{
    public static TriggerHandlerResult From(ComponentOperationResult result, ITriggerRaiser raiser)
    {
        foreach (var next in result.NextTriggers)
        {
            raiser.RaiseTrigger(
                next.TriggerCode,
                next.QueueName,
                next.TargetComponent,
                next.PayloadJson,
                next.IdempotencyKey);
        }

        return result.ResultCode switch
        {
            TriggerResultCode.Success => TriggerHandlerResult.Success(result.ResultJson, message: result.Message),
            TriggerResultCode.Retry => TriggerHandlerResult.Retry(result.Message ?? "Retry requested."),
            TriggerResultCode.Failure => TriggerHandlerResult.Failure(result.Message ?? "Failure."),
            TriggerResultCode.Compensation => TriggerHandlerResult.Compensation(result.Message ?? "Compensation."),
            _ => TriggerHandlerResult.Failure($"Unknown result code {result.ResultCode}.")
        };
    }
}
