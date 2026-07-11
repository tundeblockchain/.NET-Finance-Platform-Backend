using Microsoft.Extensions.Options;

namespace FinancePlatform.Worker;

/// <summary>
/// Phase 0 placeholder host for the service broker / batch worker.
/// Queue polling and trigger execution are implemented in Phase 2+.
/// </summary>
public sealed class BrokerHostedService(
    ILogger<BrokerHostedService> logger,
    IOptions<BrokerOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var broker = options.Value;

        logger.LogInformation(
            "FinancePlatform Worker starting. InstanceId={WorkerInstanceId}, Queues={QueueCount}",
            broker.WorkerInstanceId,
            broker.Queues.Count);

        foreach (var queue in broker.Queues)
        {
            logger.LogInformation(
                "Configured queue {QueueName} with MaxConcurrency={MaxConcurrency}, PollIntervalMs={PollIntervalMilliseconds}",
                queue.Name,
                queue.MaxConcurrency,
                queue.PollIntervalMilliseconds);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Broker idle — trigger engine not yet implemented (Phase 2).");
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
