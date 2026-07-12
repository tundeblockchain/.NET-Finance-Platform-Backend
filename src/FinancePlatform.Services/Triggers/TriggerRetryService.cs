using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancePlatform.Services.Triggers;

public sealed class TriggerRetryService(
    ITriggerStore triggerStore,
    IOptions<TriggerRetryOptions> options,
    TimeProvider timeProvider,
    ILogger<TriggerRetryService> logger)
{
    private readonly Random _random = Random.Shared;

    public async Task ScheduleRetryAsync(
        SystemEventTrigger trigger,
        string error,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var delay = RetryBackoffCalculator.Calculate(
            trigger.AttemptCount,
            TimeSpan.FromMilliseconds(settings.BaseDelayMilliseconds),
            TimeSpan.FromMilliseconds(settings.MaxDelayMilliseconds),
            settings.JitterFactor,
            _random);

        var nextAttempt = timeProvider.GetUtcNow().Add(delay);

        await triggerStore.RetryAsync(trigger.Id, error, nextAttempt, cancellationToken);

        logger.LogWarning(
            "Scheduled retry for trigger {TriggerId} attempt={AttemptCount} nextAttempt={NextAttemptUtc}: {Error}",
            trigger.Id,
            trigger.AttemptCount,
            nextAttempt,
            error);
    }
}
