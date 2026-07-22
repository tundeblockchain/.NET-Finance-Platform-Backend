using System.Collections.Concurrent;
using FinancePlatform.Data.Triggers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancePlatform.Services.Triggers;

public sealed class TriggerHeartbeatService(
    ITriggerStore triggerStore,
    TimeProvider timeProvider,
    IOptions<TriggerRecoveryOptions> recoveryOptions,
    WorkerHealthTracker healthTracker,
    ILogger<TriggerHeartbeatService> logger)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastLogUtcByQueue = new();

    public bool IsHealthy => healthTracker.IsHealthy;

    public async Task BeatQueueAsync(string workerInstanceId, string queueName, CancellationToken cancellationToken = default)
    {
        if (!healthTracker.IsHealthy)
        {
            logger.LogWarning(
                "Skipping queue heartbeat; worker unhealthy reason={Reason} queue={QueueName}",
                healthTracker.UnhealthyReason,
                queueName);
            return;
        }

        var configuredSeconds = recoveryOptions.Value.QueueHeartbeatLogIntervalSeconds;
        if (configuredSeconds <= 0)
        {
            return;
        }

        var logInterval = TimeSpan.FromSeconds(configuredSeconds);
        var now = timeProvider.GetUtcNow();
        if (_lastLogUtcByQueue.TryGetValue(queueName, out var lastLogUtc)
            && now - lastLogUtc < logInterval)
        {
            return;
        }

        _lastLogUtcByQueue[queueName] = now;
        logger.LogDebug(
            "Queue heartbeat worker={WorkerInstanceId} queue={QueueName}",
            workerInstanceId,
            queueName);
        await Task.CompletedTask;
    }

    public async Task BeatTriggerAsync(
        Guid triggerId,
        string workerInstanceId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await triggerStore.HeartbeatAsync(triggerId, workerInstanceId, leaseDuration, cancellationToken);
            healthTracker.MarkHealthy();
        }
        catch (Exception ex)
        {
            healthTracker.MarkUnhealthy($"Trigger heartbeat failed for {triggerId}: {ex.Message}");
            logger.LogError(
                ex,
                "Trigger heartbeat failed trigger={TriggerId} worker={WorkerInstanceId}",
                triggerId,
                workerInstanceId);
            throw;
        }
    }
}
