using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Brokers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Investment;
using FinancePlatform.Services.Triggers;
using FinancePlatform.Services.Workflows;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinancePlatform.UnitTests.Workflows;

public class WorkflowEnqueueServiceTests
{
    [Fact]
    public async Task Enqueue_legacy_and_customer_workflows_create_expected_root_triggers()
    {
        var store = new InMemoryTriggerStore();
        var claim = new TriggerClaimService(store, NullLogger<TriggerClaimService>.Instance);
        var directory = new InMemoryCustomerDirectory();
        var instructions = new InMemoryInvestmentInstructionStore();
        var cash = new InMemoryCashService();
        var service = new WorkflowEnqueueService(
            claim,
            directory,
            instructions,
            cash,
            new SimulatedBrokerTradingProvider());
        var accountId = Guid.NewGuid();
        var customerAccountId = Guid.NewGuid();

        var provisioned = directory.CreateCustomer(new CreateCustomerRequest
        {
            Email = "wf@example.com",
            FirstName = "Work",
            LastName = "Flow",
            Currency = "GBP"
        });
        var tradingAccountId = provisioned.TradingAccount.Id;
        directory.TryCreditTradingAccount(tradingAccountId, 500m, Guid.NewGuid(), "seed-trading");
        var seedTriggerId = Guid.NewGuid();
        cash.TryAcquireLock(tradingAccountId, "GBP", seedTriggerId, Guid.NewGuid(), TimeSpan.FromMinutes(1));
        cash.TryDeposit("seed-cash", tradingAccountId, "GBP", 500m, seedTriggerId);
        cash.TryReleaseLock(tradingAccountId, "GBP", seedTriggerId);

        var deposit = await service.EnqueueDepositAsync(new DepositWorkflowCommand
        {
            AccountId = accountId,
            Amount = 100m,
            IdempotencyKey = "api-dep-1"
        });

        var buy = await service.EnqueueBuyAsync(new BuyWorkflowCommand
        {
            CustomerId = provisioned.Customer.Id,
            AccountId = tradingAccountId,
            AssetSymbol = "VWRL",
            Quantity = 1m,
            IdempotencyKey = "api-buy-1"
        });

        var sell = await service.EnqueueSellAsync(new SellWorkflowCommand
        {
            CustomerId = provisioned.Customer.Id,
            AccountId = tradingAccountId,
            AssetSymbol = "VWRL",
            Quantity = 1m,
            IdempotencyKey = "api-sell-1"
        });

        var customerDeposit = await service.EnqueueCustomerDepositAsync(new CustomerDepositWorkflowCommand
        {
            CustomerId = 1,
            CustomerAccountId = customerAccountId,
            Amount = 250m,
            IdempotencyKey = "api-cust-dep-1"
        });

        var customerDistribute = await service.EnqueueCustomerDistributeAsync(new CustomerDistributeWorkflowCommand
        {
            CustomerId = 1,
            CustomerAccountId = customerAccountId,
            TradingAccountId = Guid.NewGuid(),
            Amount = 200m,
            IdempotencyKey = "api-cust-dist-1"
        });

        var tradingTransfer = await service.EnqueueTradingTransferToCustomerAsync(
            new TradingTransferToCustomerWorkflowCommand
            {
                CustomerId = 1,
                TradingAccountId = Guid.NewGuid(),
                CustomerAccountId = customerAccountId,
                Amount = 75m,
                IdempotencyKey = "api-xfer-back-1"
            });

        deposit.TriggerCode.Should().Be(TriggerCodes.DepositCash);
        deposit.QueueName.Should().Be(QueueNames.Cash);

        buy.TriggerCode.Should().Be(TriggerCodes.InvestmentReceiveMoney);
        buy.QueueName.Should().Be(QueueNames.Investment);
        buy.ExternalType.Should().Be(FinancePlatform.Models.Enums.ExternalEntityType.InvestmentAccount);

        sell.TriggerCode.Should().Be(TriggerCodes.InvestmentInvestMoney);
        sell.QueueName.Should().Be(QueueNames.Investment);

        customerDeposit.TriggerCode.Should().Be(TriggerCodes.CustomerDepositMoney);
        customerDeposit.QueueName.Should().Be(QueueNames.Customer);

        customerDistribute.TriggerCode.Should().Be(TriggerCodes.CustomerDistributeMoney);
        customerDistribute.QueueName.Should().Be(QueueNames.Customer);

        tradingTransfer.TriggerCode.Should().Be(TriggerCodes.TradingTransferToCustomer);
        tradingTransfer.QueueName.Should().Be(QueueNames.Trading);

        store.GetAll().Should().HaveCount(6);
        instructions.GetByIdempotencyKey("api-buy-1:instruction").Should().NotBeNull();
        instructions.GetByIdempotencyKey("api-sell-1:instruction").Should().NotBeNull();
    }
}
