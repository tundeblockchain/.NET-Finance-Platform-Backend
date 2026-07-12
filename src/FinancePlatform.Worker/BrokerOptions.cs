namespace FinancePlatform.Worker;

public sealed class BrokerOptions
{
    public const string SectionName = "Broker";

    public string WorkerInstanceId { get; set; } = Environment.MachineName;

    public int LeaseDurationSeconds { get; set; } = 30;

    public bool SeedSampleWorkflowOnStartup { get; set; } = true;

    public IReadOnlyList<QueueOptions> Queues { get; set; } = [];
}
