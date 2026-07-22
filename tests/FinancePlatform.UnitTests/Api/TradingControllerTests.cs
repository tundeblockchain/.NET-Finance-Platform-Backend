using FinancePlatform.Api.Contracts;
using FinancePlatform.Api.Controllers;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Portfolio;
using FinancePlatform.Services.Workflows;
using FinancePlatform.UnitTests.Api.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace FinancePlatform.UnitTests.Api;

public class TradingControllerTests
{
    [Fact]
    public void GetFunds_returns_not_found_when_customer_missing()
    {
        var customers = Substitute.For<ICustomerService>();
        customers.GetCustomer(9).Returns((CustomerProvisioningResult?)null);
        var controller = CreateController(customers);

        var result = controller.GetFunds(9);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void GetFunds_returns_cash_and_positions()
    {
        var provisioned = ApiTestFixtures.CreateProvisionedCustomer(tradingSettled: 400m);
        var customers = Substitute.For<ICustomerService>();
        customers.GetCustomer(1).Returns(provisioned);

        var portfolio = Substitute.For<IPortfolioService>();
        portfolio.GetPortfolio(provisioned.TradingAccount.Id, provisioned.TradingAccount.Currency)
            .Returns(new PortfolioSnapshot(
                provisioned.TradingAccount.Id,
                provisioned.TradingAccount.Currency,
                CashAvailable: 400m,
                PositionsMarketValue: 225m,
                TotalEquity: 625m,
                Positions:
                [
                    new PortfolioPositionLine("VWRL", 3m, 75m, 225m, DateTimeOffset.UtcNow, "TradeFill", "Simulated")
                ]));

        var controller = CreateController(customers, portfolioService: portfolio);

        var result = controller.GetFunds(1);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<TradingFundsResponse>().Subject;
        body.Cash.Settled.Should().Be(400m);
        body.PositionsMarketValue.Should().Be(225m);
        body.TotalEquity.Should().Be(body.Cash.Available + 225m);
        body.Positions.Should().ContainSingle(p => p.AssetSymbol == "VWRL" && p.Quantity == 3m && p.LastPrice == 75m);
    }

    [Fact]
    public void GetHistory_maps_orders_to_trade_history()
    {
        var provisioned = ApiTestFixtures.CreateProvisionedCustomer();
        var customers = Substitute.For<ICustomerService>();
        customers.GetCustomer(1).Returns(provisioned);

        var now = DateTimeOffset.UtcNow;
        var orders = Substitute.For<IOrderService>();
        orders.GetByAccount(provisioned.TradingAccount.Id).Returns(
        [
            new Order
            {
                Id = Guid.NewGuid(),
                AccountId = provisioned.TradingAccount.Id,
                TriggerId = Guid.NewGuid(),
                AssetSymbol = "VWRL",
                Side = OrderSide.Buy,
                Quantity = 2m,
                FillPrice = 75m,
                Provider = "Simulated",
                ExternalOrderId = "sim-buy-1:order",
                Status = OrderStatus.Filled,
                IdempotencyKey = "buy-1:order",
                CreatedUtc = now,
                SubmittedUtc = now,
                FilledUtc = now,
                DateModified = now
            }
        ]);

        var controller = CreateController(customers, orderService: orders);

        var result = controller.GetHistory(1);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeAssignableTo<IReadOnlyList<TradeHistoryItemResponse>>().Subject;
        body.Should().ContainSingle(h =>
            h.AssetSymbol == "VWRL"
            && h.Side == "Buy"
            && h.Quantity == 2m
            && h.FillPrice == 75m
            && h.Provider == "Simulated");
    }

    [Fact]
    public async Task Buy_returns_bad_request_when_trading_account_mismatches()
    {
        var provisioned = ApiTestFixtures.CreateProvisionedCustomer();
        var customers = Substitute.For<ICustomerService>();
        customers.GetCustomer(1).Returns(provisioned);
        var controller = CreateController(customers);

        var result = await controller.Buy(
            1,
            new TradingOrderRequest("VWRL", 1m, TradingAccountId: Guid.NewGuid()),
            CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Buy_enqueues_against_trading_account()
    {
        var provisioned = ApiTestFixtures.CreateProvisionedCustomer(tradingSettled: 500m);
        var customers = Substitute.For<ICustomerService>();
        customers.GetCustomer(1).Returns(provisioned);

        var trigger = ApiTestFixtures.CreateTrigger(TriggerCodes.BuyAsset, QueueNames.Trading, "buy-1");
        var workflows = Substitute.For<IWorkflowEnqueueService>();
        workflows.EnqueueBuyAsync(Arg.Any<BuyWorkflowCommand>(), Arg.Any<CancellationToken>()).Returns(trigger);

        var controller = CreateController(customers, workflows);

        var result = await controller.Buy(
            1,
            new TradingOrderRequest("VWRL", 2m),
            CancellationToken.None);

        var accepted = result.Result.Should().BeOfType<AcceptedResult>().Subject;
        var body = accepted.Value.Should().BeOfType<WorkflowAcceptedResponse>().Subject;
        body.TriggerCode.Should().Be(TriggerCodes.BuyAsset);

        await workflows.Received(1).EnqueueBuyAsync(
            Arg.Is<BuyWorkflowCommand>(c =>
                c.AccountId == provisioned.TradingAccount.Id
                && c.AssetSymbol == "VWRL"
                && c.Quantity == 2m
                && c.ExternalType == ExternalEntityType.TradingAccount
                && c.IdempotencyKey.StartsWith("buy-")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sell_enqueues_against_trading_account()
    {
        var provisioned = ApiTestFixtures.CreateProvisionedCustomer();
        var customers = Substitute.For<ICustomerService>();
        customers.GetCustomer(1).Returns(provisioned);

        var trigger = ApiTestFixtures.CreateTrigger(TriggerCodes.SellAsset, QueueNames.Trading, "sell-1");
        var workflows = Substitute.For<IWorkflowEnqueueService>();
        workflows.EnqueueSellAsync(Arg.Any<SellWorkflowCommand>(), Arg.Any<CancellationToken>()).Returns(trigger);

        var controller = CreateController(customers, workflows);

        var result = await controller.Sell(
            1,
            new TradingOrderRequest("VWRL", 1m),
            CancellationToken.None);

        result.Result.Should().BeOfType<AcceptedResult>();
        await workflows.Received(1).EnqueueSellAsync(
            Arg.Is<SellWorkflowCommand>(c =>
                c.AccountId == provisioned.TradingAccount.Id
                && c.ExternalType == ExternalEntityType.TradingAccount
                && c.IdempotencyKey.StartsWith("sell-")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransferToCustomer_enqueues_7003()
    {
        var provisioned = ApiTestFixtures.CreateProvisionedCustomer(tradingSettled: 400m);
        var customers = Substitute.For<ICustomerService>();
        customers.GetCustomer(1).Returns(provisioned);

        var trigger = ApiTestFixtures.CreateTrigger(TriggerCodes.TradingTransferToCustomer, QueueNames.Trading, "xfer-1");
        var workflows = Substitute.For<IWorkflowEnqueueService>();
        workflows.EnqueueTradingTransferToCustomerAsync(
                Arg.Any<TradingTransferToCustomerWorkflowCommand>(),
                Arg.Any<CancellationToken>())
            .Returns(trigger);

        var controller = CreateController(customers, workflows);

        var result = await controller.TransferToCustomer(
            1,
            new TradingTransferToCustomerHttpRequest(150m, "xfer-1"),
            CancellationToken.None);

        var accepted = result.Result.Should().BeOfType<AcceptedResult>().Subject;
        var body = accepted.Value.Should().BeOfType<WorkflowAcceptedResponse>().Subject;
        body.TriggerCode.Should().Be(TriggerCodes.TradingTransferToCustomer);

        await workflows.Received(1).EnqueueTradingTransferToCustomerAsync(
            Arg.Is<TradingTransferToCustomerWorkflowCommand>(c =>
                c.CustomerId == 1
                && c.TradingAccountId == provisioned.TradingAccount.Id
                && c.CustomerAccountId == provisioned.CustomerAccount.Id
                && c.Amount == 150m),
            Arg.Any<CancellationToken>());
    }

    private static TradingController CreateController(
        ICustomerService customers,
        IWorkflowEnqueueService? workflows = null,
        IOrderService? orderService = null,
        IPortfolioService? portfolioService = null) =>
        new(
            customers,
            workflows ?? Substitute.For<IWorkflowEnqueueService>(),
            orderService ?? Substitute.For<IOrderService>(),
            portfolioService ?? CreateEmptyPortfolioService());

    private static IPortfolioService CreateEmptyPortfolioService()
    {
        var portfolio = Substitute.For<IPortfolioService>();
        portfolio.GetPortfolio(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(call => new PortfolioSnapshot(
                call.ArgAt<Guid>(0),
                call.ArgAt<string>(1),
                0m,
                0m,
                0m,
                []));
        return portfolio;
    }
}
