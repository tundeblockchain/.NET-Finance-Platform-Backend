using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.UnitTests.Triggers.Support;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Trading;

public class TradingTransferToCustomerTests
{
    [Fact]
    public async Task Transfer_from_trading_to_customer_credits_customer_and_stops()
    {
        var harness = TriggerExecutionTestHarness.Create();
        var provisioned = harness.Customer.CreateCustomer(new CreateCustomerRequest
        {
            Email = "xfer@example.com",
            FirstName = "X",
            LastName = "Fer",
            Currency = "GBP"
        });

        var customerAccountId = provisioned.CustomerAccount.Id;
        var tradingAccountId = provisioned.TradingAccount.Id;
        var rootId = Guid.NewGuid();

        harness.Directory.TryCreditTradingAccount(
            tradingAccountId,
            300m,
            Guid.NewGuid(),
            "seed-trading");

        await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.TradingTransferToCustomer,
            QueueName = QueueNames.Trading,
            PayloadJson = JsonSerializer.Serialize(new TradingTransferToCustomerRequest
            {
                CustomerId = provisioned.Customer.Id,
                TradingAccountId = tradingAccountId,
                CustomerAccountId = customerAccountId,
                Amount = 200m,
                Currency = "GBP"
            }),
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            ExternalId = tradingAccountId,
            ExternalType = ExternalEntityType.TradingAccount,
            SourceComponent = "Api",
            TargetComponent = "Trading",
            IdempotencyKey = "xfer-back-1"
        });

        for (var i = 0; i < 8; i++)
        {
            foreach (var queue in new[] { QueueNames.Trading, QueueNames.Customer })
            {
                var claimed = await harness.Store.TryClaimAsync(queue, $"w-{queue}", TimeSpan.FromSeconds(30));
                if (claimed is not null)
                {
                    await harness.Execution.ExecuteAsync(claimed);
                }
            }
        }

        var all = harness.Store.GetAll();
        all.Should().Contain(t =>
            t.TriggerCode == TriggerCodes.TradingTransferToCustomer && t.Status == TriggerStatus.Completed);
        all.Should().Contain(t =>
            t.TriggerCode == TriggerCodes.CustomerReceiveMoney && t.Status == TriggerStatus.Completed);

        harness.Directory.GetTradingSettled(tradingAccountId).Should().Be(100m);
        harness.Directory.GetCustomerSettled(customerAccountId).Should().Be(200m);
    }
}
