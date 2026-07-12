namespace FinancePlatform.Services.Triggers;

/// <summary>
/// Broker recovery / heartbeat settings under Broker section.
/// </summary>
public sealed class TriggerRecoveryOptions
{
    public const string SectionName = "Broker";

    /// <summary>
    /// How often the recovery scanner runs.
    /// </summary>
    public int RecoveryPollIntervalMilliseconds { get; set; } = 2000;

    public int RecoveryBatchSize { get; set; } = 50;

    /// <summary>
    /// How often a running trigger lease is refreshed while an EP executes.
    /// </summary>
    public int TriggerHeartbeatIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// How often queue-alive heartbeat logs are emitted.
    /// </summary>
    public int QueueHeartbeatLogIntervalSeconds { get; set; } = 5;
}
