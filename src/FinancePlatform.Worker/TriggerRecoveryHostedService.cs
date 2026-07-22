using FinancePlatform.Services.Triggers;
using Microsoft.Extensions.Options;

namespace FinancePlatform.Worker;

/// <summary>
/// Periodically recovers triggers with expired working leases.
/// </summary>
public sealed class TriggerRecoveryHostedService(
    TriggerRecoveryService recoveryService,
    IOptions<TriggerRecoveryOptions> options,
    ILogger<TriggerRecoveryHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollMs = Math.Max(200, options.Value.RecoveryPollIntervalMilliseconds);
        logger.LogDebug("Trigger recovery scanner started (poll={PollMs}ms)", pollMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await recoveryService.RecoverOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Trigger recovery scan failed");
            }

            try
            {
                await Task.Delay(pollMs, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
