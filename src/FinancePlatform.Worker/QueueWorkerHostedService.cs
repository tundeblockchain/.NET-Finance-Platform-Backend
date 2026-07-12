using FinancePlatform.Data.Triggers;
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
    private readonly WorkerHealthTracker _healthTracker;
    private readonly IOptions<TriggerRecoveryOptions> _recoveryOptions;
    private readonly QueueOptions _queue;
    private readonly string _workerInstanceId;
    private readonly TimeSpan _leaseDuration;

    public QueueWorkerHostedService(
        ILogger<QueueWorkerHostedService> logger,
        TriggerClaimService claimService,
        TriggerExecutionService executionService,
        TriggerHeartbeatService heartbeatService,
        WorkerHealthTracker healthTracker,
        IOptions<BrokerOptions> brokerOptions,
        IOptions<TriggerRecoveryOptions> recoveryOptions,
        QueueOptions queue)
    {
        _logger = logger;
        _claimService = claimService;
        _executionService = executionService;
        _heartbeatService = heartbeatService;
        _healthTracker = healthTracker;
        _recoveryOptions = recoveryOptions;
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
                if (!_healthTracker.IsHealthy)
                {
                    _logger.LogWarning(
                        "Worker unhealthy; pausing claims on {QueueName}: {Reason}",
                        _queue.Name,
                        _healthTracker.UnhealthyReason);
                    await Task.Delay(_queue.PollIntervalMilliseconds, stoppingToken);
                    continue;
                }

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

                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = claimed.Trigger.CorrelationId,
                    ["RootWorkflowId"] = claimed.Trigger.RootWorkflowId,
                    ["TriggerId"] = claimed.Trigger.Id,
                    ["QueueName"] = _queue.Name
                }))
                {
                    await ExecuteWithLeaseHeartbeatAsync(claimed, instanceId, stoppingToken);
                }
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

    private async Task ExecuteWithLeaseHeartbeatAsync(
        ClaimedTrigger claimed,
        string instanceId,
        CancellationToken stoppingToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var heartbeatInterval = TimeSpan.FromSeconds(
            Math.Max(1, _recoveryOptions.Value.TriggerHeartbeatIntervalSeconds));

        var heartbeatTask = RefreshLeaseWhileExecutingAsync(
            claimed.Trigger.Id,
            instanceId,
            heartbeatInterval,
            linkedCts);

        try
        {
            await _heartbeatService.BeatTriggerAsync(
                claimed.Trigger.Id,
                instanceId,
                _leaseDuration,
                stoppingToken);

            await _executionService.ExecuteAsync(claimed, linkedCts.Token);
        }
        finally
        {
            if (!linkedCts.IsCancellationRequested)
            {
                await linkedCts.CancelAsync();
            }

            try
            {
                await heartbeatTask;
            }
            catch (OperationCanceledException)
            {
                // expected when execution finishes
            }
        }
    }

    private async Task RefreshLeaseWhileExecutingAsync(
        Guid triggerId,
        string instanceId,
        TimeSpan interval,
        CancellationTokenSource linkedCts)
    {
        var cancellationToken = linkedCts.Token;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, cancellationToken);
                await _heartbeatService.BeatTriggerAsync(
                    triggerId,
                    instanceId,
                    _leaseDuration,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                if (!linkedCts.IsCancellationRequested)
                {
                    await linkedCts.CancelAsync();
                }

                break;
            }
        }
    }
}
