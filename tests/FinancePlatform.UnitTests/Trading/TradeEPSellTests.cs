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

public class TradeEPSellTests
{
    [Fact]
    public async Task Sell_creates_instruction_and_raises_investment_invest()
    {
        var directory = new InMemoryCustomerDirectory();
        var provisioned = directory.CreateCustomer(new CreateCustomerRequest
        {
            Email = "sell@example.com",
            FirstName = "Sell",
            LastName = "Test",
            Currency = "GBP"
        });

        var investmentAccount = directory.EnsureInvestmentAccount(
            provisioned.Customer.Id,
            provisioned.TradingAccount.Id,
            "GBP");

        var positions = new InMemoryPositionService();
        positions.TryApplyBuy("seed-position", investmentAccount.Id, "VWRL", 3m);

        var instructions = new InMemoryInvestmentInstructionStore();
        var trade = new TradeService(
            new InMemoryCashService(),
            new InMemoryLedgerService(),
            positions,
            directory,
            instructions);
        var ep = new TradeEP(trade);

        var raiser = new TriggerRaiseBuffer();
        var result = await ep.ProcessAsync(
            CreateContext(provisioned.TradingAccount.Id, "sell-1"),
            TriggerCodes.SellAsset,
            JsonSerializer.Serialize(new TradeAssetRequest
            {
                AssetSymbol = "VWRL",
                Quantity = 2m,
                Currency = "GBP",
                CashAmount = 180m
            }),
            raiser,
            CancellationToken.None);

        result.ResultCode.Should().Be(TriggerResultCode.Success);
        raiser.Raised.Should().ContainSingle(t => t.TriggerCode == TriggerCodes.InvestmentInvestMoney);
    }

    private static TriggerContext CreateContext(Guid accountId, string idempotencyKey) => new()
    {
        TriggerId = Guid.NewGuid(),
        RootWorkflowId = Guid.NewGuid(),
        CorrelationId = Guid.NewGuid(),
        ExternalId = accountId,
        ExternalType = ExternalEntityType.TradingAccount,
        SourceComponent = "Api",
        TargetComponent = "Trading",
        IdempotencyKey = new(idempotencyKey)
    };
}
