using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Triggers;
using FinancePlatform.Services.Workflows;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinancePlatform.UnitTests.Workflows;

public class WorkflowEnqueueServiceTests
{
    [Fact]
    public async Task Enqueue_deposit_buy_sell_and_allocation_create_expected_root_triggers()
    {
        var store = new InMemoryTriggerStore();
        var claim = new TriggerClaimService(store, NullLogger<TriggerClaimService>.Instance);
        var service = new WorkflowEnqueueService(claim);
        var accountId = Guid.NewGuid();

        var deposit = await service.EnqueueDepositAsync(new DepositWorkflowCommand
        {
            AccountId = accountId,
            Amount = 100m,
            IdempotencyKey = "api-dep-1"
        });

        var buy = await service.EnqueueBuyAsync(new BuyWorkflowCommand
        {
            AccountId = accountId,
            AssetSymbol = "VWRL",
            Quantity = 1m,
            CashAmount = 50m,
            IdempotencyKey = "api-buy-1"
        });

        var sell = await service.EnqueueSellAsync(new SellWorkflowCommand
        {
            AccountId = accountId,
            AssetSymbol = "VWRL",
            Quantity = 1m,
            CashAmount = 50m,
            IdempotencyKey = "api-sell-1"
        });

        var allocation = await service.EnqueueAllocationAsync(new AllocationWorkflowCommand
        {
            AccountId = accountId,
            Amount = 200m,
            Quantity = 2m,
            IdempotencyKey = "api-alloc-1"
        });

        deposit.TriggerCode.Should().Be(TriggerCodes.DepositCash);
        deposit.QueueName.Should().Be(QueueNames.Cash);

        buy.TriggerCode.Should().Be(TriggerCodes.BuyAsset);
        buy.QueueName.Should().Be(QueueNames.Trading);

        sell.TriggerCode.Should().Be(TriggerCodes.SellAsset);
        sell.QueueName.Should().Be(QueueNames.Trading);

        allocation.TriggerCode.Should().Be(TriggerCodes.CustomerDistributeMoney);
        allocation.QueueName.Should().Be(QueueNames.Customer);

        store.GetAll().Should().HaveCount(4);
    }
}
