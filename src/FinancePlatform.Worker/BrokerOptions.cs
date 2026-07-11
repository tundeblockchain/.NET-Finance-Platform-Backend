namespace FinancePlatform.Worker;

public sealed class BrokerOptions
{
    public const string SectionName = "Broker";

    public string WorkerInstanceId { get; set; } = Environment.MachineName;

    public IReadOnlyList<QueueOptions> Queues { get; set; } = [];
}

public sealed class QueueOptions
{
    public string Name { get; set; } = string.Empty;

    public int MaxConcurrency { get; set; } = 1;

    public int PollIntervalMilliseconds { get; set; } = 1000;
}
