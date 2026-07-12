namespace FinancePlatform.Worker;

public sealed class QueueOptions
{
    public string Name { get; set; } = string.Empty;

    public int MaxConcurrency { get; set; } = 1;

    public int PollIntervalMilliseconds { get; set; } = 1000;
}
