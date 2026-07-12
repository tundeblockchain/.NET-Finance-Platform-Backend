namespace FinancePlatform.Services.Triggers;

/// <summary>
/// Exponential backoff with symmetric jitter for trigger retries.
/// </summary>
public static class RetryBackoffCalculator
{
    public static TimeSpan Calculate(
        int attemptCount,
        TimeSpan baseDelay,
        TimeSpan maxDelay,
        double jitterFactor,
        Random? random = null)
    {
        if (attemptCount < 1)
        {
            attemptCount = 1;
        }

        jitterFactor = Math.Clamp(jitterFactor, 0, 1);
        var exponential = baseDelay.TotalMilliseconds * Math.Pow(2, attemptCount - 1);
        var capped = Math.Min(exponential, maxDelay.TotalMilliseconds);

        if (jitterFactor <= 0 || random is null)
        {
            return TimeSpan.FromMilliseconds(capped);
        }

        var jitterRange = capped * jitterFactor;
        var jitter = (random.NextDouble() * 2 - 1) * jitterRange;
        var delayed = Math.Max(0, capped + jitter);
        return TimeSpan.FromMilliseconds(delayed);
    }
}
