namespace FinancePlatform.Services.Triggers;

public sealed class TriggerRetryOptions
{
    public const string SectionName = "Broker:Retry";

    public int BaseDelayMilliseconds { get; set; } = 500;

    public int MaxDelayMilliseconds { get; set; } = 30_000;

    /// <summary>
    /// Jitter as a fraction of the computed delay (0.2 = ±20%).
    /// </summary>
    public double JitterFactor { get; set; } = 0.2;
}
