using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Triggers;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Triggers;

public class InMemoryTriggerStoreTests
{
    [Fact]
    public async Task TryClaim_is_exclusive_across_workers()
    {
        var store = new InMemoryTriggerStore();
        await store.EnqueueAsync(CreateEnqueue("Cash", TriggerCodes.DepositCash, "idem-1"));

        var tasks = Enumerable.Range(0, 20)
            .Select(i => store.TryClaimAsync("Cash", $"worker-{i}", TimeSpan.FromSeconds(30)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        results.Count(r => r is not null).Should().Be(1);
        store.GetWorking().Should().HaveCount(1);
    }

    [Fact]
    public async Task Enqueue_with_same_idempotency_key_returns_existing_trigger()
    {
        var store = new InMemoryTriggerStore();
        var first = await store.EnqueueAsync(CreateEnqueue("Cash", TriggerCodes.DepositCash, "same-key"));
        var second = await store.EnqueueAsync(CreateEnqueue("Cash", TriggerCodes.DepositCash, "same-key"));

        second.Id.Should().Be(first.Id);
        store.GetAll().Should().HaveCount(1);
    }

    private static EnqueueTriggerCommand CreateEnqueue(string queue, int code, string idempotencyKey) => new()
    {
        TriggerCode = code,
        QueueName = queue,
        PayloadJson = "{}",
        RootWorkflowId = Guid.NewGuid(),
        CorrelationId = Guid.NewGuid(),
        SourceComponent = "Test",
        TargetComponent = "Cash",
        IdempotencyKey = idempotencyKey
    };
}
