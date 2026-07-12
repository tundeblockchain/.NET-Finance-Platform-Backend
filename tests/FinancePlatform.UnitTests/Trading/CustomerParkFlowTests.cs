using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.UnitTests.Triggers.Support;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Trading;

public class CustomerParkFlowTests
{
    [Fact]
    public async Task Deposit_then_distribute_parks_in_trading_account_without_further_chain()
    {
        var harness = TriggerExecutionTestHarness.Create();
        var provisioned = harness.Customer.CreateCustomer(new CreateCustomerRequest
        {
            Email = "park@example.com",
            FirstName = "Park",
            LastName = "Only",
            Currency = "GBP"
        });

        var rootId = Guid.NewGuid();
        var customerAccountId = provisioned.CustomerAccount.Id;
        var tradingAccountId = provisioned.TradingAccount.Id;

        await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.CustomerDepositMoney,
            QueueName = QueueNames.Customer,
            PayloadJson = JsonSerializer.Serialize(new CustomerDepositRequest
            {
                CustomerId = provisioned.Customer.Id,
                CustomerAccountId = customerAccountId,
                Amount = 500m,
                Currency = "GBP"
            }),
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            ExternalId = customerAccountId,
            ExternalType = ExternalEntityType.CustomerAccount,
            SourceComponent = "Api",
            TargetComponent = "Customer",
            IdempotencyKey = "cust-dep-1"
        });

        await DrainCustomerAndTrading(harness);

        harness.Directory.GetCustomerSettled(customerAccountId).Should().Be(500m);
        harness.Directory.GetTradingSettled(tradingAccountId).Should().Be(0m);

        await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.CustomerDistributeMoney,
            QueueName = QueueNames.Customer,
            PayloadJson = JsonSerializer.Serialize(new DistributeMoneyRequest
            {
                CustomerId = provisioned.Customer.Id,
                CustomerAccountId = customerAccountId,
                TradingAccountId = tradingAccountId,
                Amount = 400m,
                CashAmount = 400m,
                Currency = "GBP"
            }),
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            AllocationRequestId = rootId,
            ExternalId = customerAccountId,
            ExternalType = ExternalEntityType.CustomerAccount,
            SourceComponent = "Api",
            TargetComponent = "Customer",
            IdempotencyKey = "cust-dist-1"
        });

        await DrainCustomerAndTrading(harness);

        var all = harness.Store.GetAll();
        all.Should().Contain(t => t.TriggerCode == TriggerCodes.CustomerDepositMoney && t.Status == TriggerStatus.Completed);
        all.Should().Contain(t => t.TriggerCode == TriggerCodes.CustomerDistributeMoney && t.Status == TriggerStatus.Completed);
        all.Should().Contain(t => t.TriggerCode == TriggerCodes.TradingReceiveMoney && t.Status == TriggerStatus.Completed);
        all.Should().NotContain(t => t.TriggerCode == TriggerCodes.TradingDistributeMoney);
        all.Should().NotContain(t => t.TriggerCode == TriggerCodes.InvestmentReceiveMoney);

        harness.Directory.GetCustomerSettled(customerAccountId).Should().Be(100m);
        harness.Directory.GetTradingSettled(tradingAccountId).Should().Be(400m);
    }

    private static async Task DrainCustomerAndTrading(TriggerExecutionTestHarness harness)
    {
        for (var i = 0; i < 8; i++)
        {
            foreach (var queue in new[] { QueueNames.Customer, QueueNames.Trading })
            {
                var claimed = await harness.Store.TryClaimAsync(queue, $"w-{queue}", TimeSpan.FromSeconds(30));
                if (claimed is not null)
                {
                    await harness.Execution.ExecuteAsync(claimed);
                }
            }
        }
    }
}
