using FinancePlatform.Services.Triggers;
using Microsoft.Extensions.Options;

namespace FinancePlatform.Worker;

/// <summary>
/// Polls a single queue and executes claimed triggers up to MaxConcurrency.
/// </summary>
public sealed class QueueWorkerHostedService : BackgroundService
{
    private readonly ILogger<QueueWorkerHostedService> _logger;
    private readonly TriggerClaimService _claimService;
    private readonly TriggerExecutionService _executionService;
    private readonly TriggerHeartbeatService _heartbeatService;
    private readonly QueueOptions _queue;
    private readonly string _workerInstanceId;
    private readonly TimeSpan _leaseDuration;

    public QueueWorkerHostedService(
        ILogger<QueueWorkerHostedService> logger,
        TriggerClaimService claimService,
        TriggerExecutionService executionService,
        TriggerHeartbeatService heartbeatService,
        IOptions<BrokerOptions> brokerOptions,
        QueueOptions queue)
    {
        _logger = logger;
        _claimService = claimService;
        _executionService = executionService;
        _heartbeatService = heartbeatService;
        _queue = queue;
        _workerInstanceId = $"{brokerOptions.Value.WorkerInstanceId}:{queue.Name}";
        _leaseDuration = TimeSpan.FromSeconds(Math.Max(5, brokerOptions.Value.LeaseDurationSeconds));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Queue worker started for {QueueName} (maxConcurrency={MaxConcurrency})",
            _queue.Name,
            _queue.MaxConcurrency);

        var workers = Enumerable
            .Range(0, Math.Max(1, _queue.MaxConcurrency))
            .Select(i => RunWorkerAsync(i, stoppingToken));

        await Task.WhenAll(workers);
    }

    private async Task RunWorkerAsync(int workerIndex, CancellationToken stoppingToken)
    {
        var instanceId = $"{_workerInstanceId}:{workerIndex}";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _heartbeatService.BeatQueueAsync(instanceId, _queue.Name, stoppingToken);

                var claimed = await _claimService.ClaimNextAsync(
                    _queue.Name,
                    instanceId,
                    _leaseDuration,
                    stoppingToken);

                if (claimed is null)
                {
                    await Task.Delay(_queue.PollIntervalMilliseconds, stoppingToken);
                    continue;
                }

                await _heartbeatService.BeatTriggerAsync(
                    claimed.Trigger.Id,
                    instanceId,
                    _leaseDuration,
                    stoppingToken);

                await _executionService.ExecuteAsync(claimed, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queue worker error on {QueueName} worker={WorkerIndex}", _queue.Name, workerIndex);
                await Task.Delay(_queue.PollIntervalMilliseconds, stoppingToken);
            }
        }
    }
}
