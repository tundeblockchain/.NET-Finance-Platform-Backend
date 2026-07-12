using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Trading;
using FinancePlatform.Services.Triggers;
using FinancePlatform.Worker.Handlers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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

public class RetryBackoffCalculatorTests
{
    [Fact]
    public void Calculate_grows_exponentially_and_caps_at_max()
    {
        var baseDelay = TimeSpan.FromMilliseconds(100);
        var maxDelay = TimeSpan.FromMilliseconds(1000);

        var attempt1 = RetryBackoffCalculator.Calculate(1, baseDelay, maxDelay, jitterFactor: 0);
        var attempt2 = RetryBackoffCalculator.Calculate(2, baseDelay, maxDelay, jitterFactor: 0);
        var attempt5 = RetryBackoffCalculator.Calculate(5, baseDelay, maxDelay, jitterFactor: 0);

        attempt1.Should().Be(TimeSpan.FromMilliseconds(100));
        attempt2.Should().Be(TimeSpan.FromMilliseconds(200));
        attempt5.Should().Be(TimeSpan.FromMilliseconds(1000));
    }

    [Fact]
    public void Calculate_applies_deterministic_jitter_with_seeded_random()
    {
        var random = new Random(42);
        var delay = RetryBackoffCalculator.Calculate(
            attemptCount: 3,
            baseDelay: TimeSpan.FromMilliseconds(100),
            maxDelay: TimeSpan.FromMilliseconds(10_000),
            jitterFactor: 0.2,
            random);

        // 100 * 2^(3-1) = 400, jitter ±20%
        delay.TotalMilliseconds.Should().BeInRange(320, 480);
    }
}

public class TriggerExecutionServiceTests
{
    [Fact]
    public async Task Successful_handler_completes_and_enqueues_child_triggers()
    {
        var harness = CreateHarness();
        var accountId = Guid.NewGuid();

        var root = await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = "Cash",
            PayloadJson = JsonSerializer.Serialize(new DepositCashPayload
            {
                Amount = 250m,
                Currency = "GBP",
                AssetSymbol = "VWRL",
                Quantity = 2m
            }),
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            ExternalId = accountId,
            ExternalType = ExternalEntityType.Account,
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "deposit-1"
        });

        var claimed = await harness.Store.TryClaimAsync("Cash", "worker-a", TimeSpan.FromSeconds(30));
        claimed.Should().NotBeNull();

        await harness.Execution.ExecuteAsync(claimed!);

        var all = harness.Store.GetAll();
        all.Should().Contain(t => t.Id == root.Id && t.Status == TriggerStatus.Completed);
        all.Should().Contain(t =>
            t.TriggerCode == TriggerCodes.BuyAsset
            && t.QueueName == "Trading"
            && t.ParentTriggerId == root.Id);

        harness.Cash.GetBalance(accountId).Should().Be(250m);
    }

    [Fact]
    public async Task Failure_enqueues_compensation_trigger_with_negative_code()
    {
        var harness = CreateHarness(registerDeposit: false);
        harness.Registry.RegisterHandler(new AlwaysFailHandler(TriggerCodes.BuyAsset));

        var trigger = await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.BuyAsset,
            QueueName = "Trading",
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Cash",
            TargetComponent = "Trading",
            IdempotencyKey = "buy-fail-1"
        });

        var claimed = await harness.Store.TryClaimAsync("Trading", "worker-b", TimeSpan.FromSeconds(30));
        await harness.Execution.ExecuteAsync(claimed!);

        var all = harness.Store.GetAll();
        all.Should().Contain(t => t.Id == trigger.Id && t.Status == TriggerStatus.Failed);
        all.Should().Contain(t =>
            t.TriggerCode == TriggerCodes.Compensate(TriggerCodes.BuyAsset)
            && t.QueueName == "Reversal");
    }

    [Fact]
    public async Task Retry_result_reschedules_pending_trigger()
    {
        var harness = CreateHarness(registerDeposit: false);
        harness.Registry.RegisterHandler(new AlwaysRetryHandler(TriggerCodes.DepositCash));

        await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = "Cash",
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "retry-1"
        });

        var claimed = await harness.Store.TryClaimAsync("Cash", "worker-c", TimeSpan.FromSeconds(30));
        await harness.Execution.ExecuteAsync(claimed!);

        var trigger = harness.Store.GetAll().Single();
        trigger.Status.Should().Be(TriggerStatus.Pending);
        trigger.NextAttemptUtc.Should().NotBeNull();
        trigger.NextAttemptUtc.Should().BeAfter(DateTimeOffset.UtcNow.AddMilliseconds(-50));
        trigger.LastError.Should().Contain("busy");
        harness.Store.GetWorking().Should().BeEmpty();
    }

    [Fact]
    public void Idempotent_deposit_does_not_duplicate_side_effects()
    {
        var cash = new InMemoryCashService();
        var accountId = Guid.NewGuid();

        cash.TryDeposit("dep-key", accountId, 100m, "GBP").Should().BeTrue();
        cash.TryDeposit("dep-key", accountId, 100m, "GBP").Should().BeFalse();

        cash.DepositCount.Should().Be(1);
        cash.GetBalance(accountId).Should().Be(100m);
    }

    [Fact]
    public async Task Deposit_then_buy_chain_processes_end_to_end()
    {
        var harness = CreateHarness();
        var accountId = Guid.NewGuid();
        var rootId = Guid.NewGuid();

        await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = "Cash",
            PayloadJson = JsonSerializer.Serialize(new DepositCashPayload
            {
                Amount = 500m,
                Currency = "GBP",
                AssetSymbol = "VWRL",
                Quantity = 3m
            }),
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            ExternalId = accountId,
            ExternalType = ExternalEntityType.Account,
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "chain-deposit"
        });

        for (var i = 0; i < 5; i++)
        {
            var cashClaim = await harness.Store.TryClaimAsync("Cash", "w-cash", TimeSpan.FromSeconds(30));
            if (cashClaim is not null)
            {
                await harness.Execution.ExecuteAsync(cashClaim);
            }

            var tradeClaim = await harness.Store.TryClaimAsync("Trading", "w-trade", TimeSpan.FromSeconds(30));
            if (tradeClaim is not null)
            {
                await harness.Execution.ExecuteAsync(tradeClaim);
            }
        }

        harness.Store.GetAll().Should().Contain(t => t.TriggerCode == TriggerCodes.DepositCash && t.Status == TriggerStatus.Completed);
        harness.Store.GetAll().Should().Contain(t => t.TriggerCode == TriggerCodes.BuyAsset && t.Status == TriggerStatus.Completed);
        harness.Cash.GetBalance(accountId).Should().Be(500m);
        harness.Trading.GetPosition(accountId, "VWRL").Should().Be(3m);
    }

    private static TestHarness CreateHarness(bool registerDeposit = true)
    {
        var store = new InMemoryTriggerStore();
        var registry = new TriggerHandlerRegistry();
        var cash = new InMemoryCashService();
        var trading = new InMemoryTradingService();

        if (registerDeposit)
        {
            registry.RegisterHandler(new DepositCashHandler(cash));
            registry.RegisterHandler(new BuyAssetHandler(trading));
            registry.RegisterHandler(new ReverseBuyAssetHandler());
        }

        var retryOptions = Options.Create(new TriggerRetryOptions
        {
            BaseDelayMilliseconds = 10,
            MaxDelayMilliseconds = 100,
            JitterFactor = 0
        });

        var retry = new TriggerRetryService(
            store,
            retryOptions,
            TimeProvider.System,
            NullLogger<TriggerRetryService>.Instance);

        var execution = new TriggerExecutionService(
            store,
            registry,
            retry,
            NullLogger<TriggerExecutionService>.Instance);

        return new TestHarness(store, registry, cash, trading, execution);
    }

    private sealed record TestHarness(
        InMemoryTriggerStore Store,
        TriggerHandlerRegistry Registry,
        InMemoryCashService Cash,
        InMemoryTradingService Trading,
        TriggerExecutionService Execution);

    private sealed class AlwaysFailHandler(int triggerCode) : ITriggerHandler
    {
        public int TriggerCode { get; } = triggerCode;

        public Task<TriggerHandlerResult> ExecuteAsync(
            Models.Dtos.TriggerContext context,
            string payloadJson,
            CancellationToken cancellationToken) =>
            Task.FromResult(TriggerHandlerResult.Failure("forced failure"));
    }

    private sealed class AlwaysRetryHandler(int triggerCode) : ITriggerHandler
    {
        public int TriggerCode { get; } = triggerCode;

        public Task<TriggerHandlerResult> ExecuteAsync(
            Models.Dtos.TriggerContext context,
            string payloadJson,
            CancellationToken cancellationToken) =>
            Task.FromResult(TriggerHandlerResult.Retry("busy"));
    }
}
