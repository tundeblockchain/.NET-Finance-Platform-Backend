using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.UnitTests.Triggers.Support;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Trading;

public class AllocationChainTests
{
    [Fact]
    public async Task Allocation_chain_propagates_context_through_to_asset_buy()
    {
        var harness = TriggerExecutionTestHarness.Create();
        var accountId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var allocationId = Guid.NewGuid();

        await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.CustomerDistributeMoney,
            QueueName = QueueNames.Customer,
            PayloadJson = JsonSerializer.Serialize(new
            {
                Amount = 400m,
                CashAmount = 400m,
                Currency = "GBP",
                AssetSymbol = "VWRL",
                Quantity = 4m
            }),
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            AllocationRequestId = allocationId,
            ExternalId = accountId,
            ExternalType = ExternalEntityType.Account,
            SourceComponent = "Api",
            TargetComponent = "Customer",
            IdempotencyKey = "alloc-1"
        });

        for (var i = 0; i < 12; i++)
        {
            foreach (var queue in new[]
                     {
                         QueueNames.Customer, QueueNames.Trading, QueueNames.Investment, QueueNames.AssetTrading
                     })
            {
                var claimed = await harness.Store.TryClaimAsync(queue, $"w-{queue}", TimeSpan.FromSeconds(30));
                if (claimed is not null)
                {
                    await harness.Execution.ExecuteAsync(claimed);
                }
            }
        }

        var all = harness.Store.GetAll();
        all.Should().Contain(t => t.TriggerCode == TriggerCodes.CustomerDistributeMoney && t.Status == TriggerStatus.Completed);
        all.Should().Contain(t => t.TriggerCode == TriggerCodes.TradingReceiveMoney && t.Status == TriggerStatus.Completed);
        all.Should().Contain(t => t.TriggerCode == TriggerCodes.TradingDistributeMoney && t.Status == TriggerStatus.Completed);
        all.Should().Contain(t => t.TriggerCode == TriggerCodes.InvestmentReceiveMoney && t.Status == TriggerStatus.Completed);
        all.Should().Contain(t => t.TriggerCode == TriggerCodes.InvestmentInvestMoney && t.Status == TriggerStatus.Completed);
        all.Should().Contain(t => t.TriggerCode == TriggerCodes.AssetBuyAsset && t.Status == TriggerStatus.Completed);

        all.Where(t => t.ParentTriggerId is not null)
            .Should().OnlyContain(t => t.RootWorkflowId == rootId
                && t.CorrelationId == rootId
                && t.AllocationRequestId == allocationId
                && t.ExternalId == accountId);

        harness.Cash.GetSettled(accountId, "GBP").Should().Be(0m);
        harness.Trading.GetPosition(accountId, "VWRL").Should().Be(4m);
    }
}
