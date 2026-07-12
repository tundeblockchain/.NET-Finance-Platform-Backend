using FinancePlatform.Data.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancePlatform.Services.Triggers;

/// <summary>
/// Requeues triggers whose working leases have expired (crash / hung worker recovery).
/// </summary>
public sealed class TriggerRecoveryService(
    ITriggerStore triggerStore,
    IOptions<TriggerRecoveryOptions> options,
    TimeProvider timeProvider,
    ILogger<TriggerRecoveryService> logger)
{
    public async Task<IReadOnlyList<RecoveredTrigger>> RecoverOnceAsync(CancellationToken cancellationToken = default)
    {
        var batchSize = Math.Max(1, options.Value.RecoveryBatchSize);
        var recovered = await triggerStore.RecoverExpiredLeasesAsync(
            batchSize,
            nextAttemptUtc: timeProvider.GetUtcNow(),
            cancellationToken);

        foreach (var item in recovered)
        {
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = item.Trigger.CorrelationId,
                ["RootWorkflowId"] = item.Trigger.RootWorkflowId,
                ["TriggerId"] = item.Trigger.Id
            }))
            {
                logger.LogWarning(
                    "Recovered expired lease for trigger {TriggerId} code={TriggerCode} previousWorker={PreviousWorker}",
                    item.Trigger.Id,
                    item.Trigger.TriggerCode,
                    item.PreviousWorkerInstanceId);
            }
        }

        if (recovered.Count > 0)
        {
            logger.LogInformation("Lease recovery requeued {Count} trigger(s)", recovered.Count);
        }

        return recovered;
    }
}
