using FinancePlatform.Data.Triggers;
using Microsoft.Extensions.Logging;

namespace FinancePlatform.Services.Triggers;

public sealed class TriggerHeartbeatService(
    ITriggerStore triggerStore,
    ILogger<TriggerHeartbeatService> logger)
{
    public async Task BeatQueueAsync(string workerInstanceId, string queueName, CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Queue heartbeat worker={WorkerInstanceId} queue={QueueName}",
            workerInstanceId,
            queueName);
        await Task.CompletedTask;
    }

    public Task BeatTriggerAsync(
        Guid triggerId,
        string workerInstanceId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        return triggerStore.HeartbeatAsync(triggerId, workerInstanceId, leaseDuration, cancellationToken);
    }
}
