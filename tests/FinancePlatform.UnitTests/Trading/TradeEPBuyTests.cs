using System.Text.Json;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Investment;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Positions;
using FinancePlatform.Services.Trade;
using FinancePlatform.Services.Triggers;
using FinancePlatform.Worker.EventProcessors;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Trading;

public class TradeEPBuyTests
{
    [Fact]
    public async Task Buy_with_insufficient_trading_cash_fails()
    {
        var directory = new InMemoryCustomerDirectory();
        var provisioned = directory.CreateCustomer(new CreateCustomerRequest
        {
            Email = "insufficient@example.com",
            FirstName = "Low",
            LastName = "Cash",
            Currency = "GBP"
        });
        directory.TryCreditTradingAccount(provisioned.TradingAccount.Id, 25m, Guid.NewGuid(), "seed-trading");

        var (ep, cash) = CreateEp(directory);
        SeedExecutableCash(cash, provisioned.TradingAccount.Id, 25m);

        var result = await ep.ProcessAsync(
            CreateContext(provisioned.TradingAccount.Id, "buy-insufficient"),
            TriggerCodes.BuyAsset,
            JsonSerializer.Serialize(new TradeAssetRequest
            {
                AssetSymbol = "VWRL",
                Quantity = 1m,
                Currency = "GBP",
                CashAmount = 100m
            }),
            new TriggerRaiseBuffer(),
            CancellationToken.None);

        result.ResultCode.Should().Be(TriggerResultCode.Failure);
        result.Message.Should().Contain("Insufficient");
    }

    [Fact]
    public async Task Buy_creates_instruction_and_raises_investment_receive()
    {
        var directory = new InMemoryCustomerDirectory();
        var provisioned = directory.CreateCustomer(new CreateCustomerRequest
        {
            Email = "buy@example.com",
            FirstName = "Buy",
            LastName = "Ok",
            Currency = "GBP"
        });
        directory.TryCreditTradingAccount(provisioned.TradingAccount.Id, 500m, Guid.NewGuid(), "seed-trading");

        var (ep, cash) = CreateEp(directory);
        SeedExecutableCash(cash, provisioned.TradingAccount.Id, 500m);

        var raiser = new TriggerRaiseBuffer();
        var result = await ep.ProcessAsync(
            CreateContext(provisioned.TradingAccount.Id, "buy-ok"),
            TriggerCodes.BuyAsset,
            JsonSerializer.Serialize(new TradeAssetRequest
            {
                AssetSymbol = "VWRL",
                Quantity = 2m,
                Currency = "GBP",
                CashAmount = 150m
            }),
            raiser,
            CancellationToken.None);

        result.ResultCode.Should().Be(TriggerResultCode.Success);
        directory.GetTradingSettled(provisioned.TradingAccount.Id).Should().Be(350m);
        raiser.Raised.Should().ContainSingle(t => t.TriggerCode == TriggerCodes.InvestmentReceiveMoney);
    }

    private static (TradeEP Ep, InMemoryCashService Cash) CreateEp(InMemoryCustomerDirectory directory)
    {
        var cash = new InMemoryCashService();
        var ledger = new InMemoryLedgerService();
        var positions = new InMemoryPositionService();
        var instructions = new InMemoryInvestmentInstructionStore();
        var trade = new TradeService(cash, ledger, positions, directory, instructions);
        return (new TradeEP(trade), cash);
    }

    private static void SeedExecutableCash(InMemoryCashService cash, Guid accountId, decimal amount)
    {
        var triggerId = Guid.NewGuid();
        cash.TryAcquireLock(accountId, "GBP", triggerId, Guid.NewGuid(), TimeSpan.FromMinutes(1));
        cash.TryDeposit("seed", accountId, "GBP", amount, triggerId);
        cash.TryReleaseLock(accountId, "GBP", triggerId);
    }

    private static TriggerContext CreateContext(Guid accountId, string key) => new()
    {
        TriggerId = Guid.NewGuid(),
        RootWorkflowId = Guid.NewGuid(),
        CorrelationId = Guid.NewGuid(),
        ExternalId = accountId,
        ExternalType = ExternalEntityType.TradingAccount,
        SourceComponent = "Api",
        TargetComponent = "Trading",
        IdempotencyKey = new(key)
    };
}
