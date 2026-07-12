using System.Collections.Concurrent;
using FinancePlatform.Data.Triggers;
using Microsoft.Extensions.Logging;

namespace FinancePlatform.Services.Triggers;

public sealed class TriggerHeartbeatService(
    ITriggerStore triggerStore,
    TimeProvider timeProvider,
    ILogger<TriggerHeartbeatService> logger)
{
    private static readonly TimeSpan LogInterval = TimeSpan.FromSeconds(5);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastLogUtcByQueue = new();

    public Task BeatQueueAsync(string workerInstanceId, string queueName, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        if (_lastLogUtcByQueue.TryGetValue(queueName, out var lastLogUtc)
            && now - lastLogUtc < LogInterval)
        {
            return Task.CompletedTask;
        }

        _lastLogUtcByQueue[queueName] = now;
        logger.LogDebug(
            "Queue heartbeat worker={WorkerInstanceId} queue={QueueName}",
            workerInstanceId,
            queueName);

        return Task.CompletedTask;
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
