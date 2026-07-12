using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Entities;
using Microsoft.Extensions.Logging;

namespace FinancePlatform.Services.Triggers;

public sealed class TriggerClaimService(
    ITriggerStore triggerStore,
    ILogger<TriggerClaimService> logger)
{
    public Task<ClaimedTrigger?> ClaimNextAsync(
        string queueName,
        string workerInstanceId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        return triggerStore.TryClaimAsync(queueName, workerInstanceId, leaseDuration, cancellationToken);
    }

    public async Task<SystemEventTrigger> EnqueueAsync(
        EnqueueTriggerCommand command,
        CancellationToken cancellationToken = default)
    {
        var trigger = await triggerStore.EnqueueAsync(command, cancellationToken);
        logger.LogInformation(
            "Enqueued trigger {TriggerId} code={TriggerCode} queue={QueueName} idempotency={IdempotencyKey}",
            trigger.Id,
            trigger.TriggerCode,
            trigger.QueueName,
            trigger.IdempotencyKey);
        return trigger;
    }
}
