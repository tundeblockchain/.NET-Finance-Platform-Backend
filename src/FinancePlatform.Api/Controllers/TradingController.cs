using FinancePlatform.Api.Contracts;
using FinancePlatform.Models.Enums;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Portfolio;
using FinancePlatform.Services.Workflows;
using Microsoft.AspNetCore.Mvc;

namespace FinancePlatform.Api.Controllers;

[ApiController]
[Route("api/trading/customers/{customerId:int}")]
[Tags("Trading")]
public sealed class TradingController(
    ICustomerService customerService,
    ICustomerDirectory customerDirectory,
    IWorkflowEnqueueService workflowsService,
    IOrderService orderService,
    IPortfolioService portfolioService) : ControllerBase
{
    [HttpGet("funds")]
    [EndpointName("GetTradingFunds")]
    [EndpointSummary("View trading funds")]
    [EndpointDescription("Returns parked cash, open positions, and mark-to-market totals from stored prices.")]
    public ActionResult<TradingFundsResponse> GetFunds(int customerId)
    {
        var customer = customerService.GetCustomer(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var tradingAccount = customer.TradingAccount;
        var portfolio = portfolioService.GetPortfolio(
            tradingAccount.Id,
            tradingAccount.Currency);

        // Available must reflect CashBalance (what buys can reserve), not TradingAccount+Investment totals.
        var cash = CustomerMapper.ToTradingAccountBalance(tradingAccount, portfolio.CashAvailable);
        var positions = portfolio.Positions
            .Select(p => new PositionResponse(
                p.AssetSymbol,
                p.Quantity,
                p.LastPrice,
                p.MarketValue,
                p.PriceObservedUtc))
            .ToArray();

        return Ok(new TradingFundsResponse(
            cash,
            positions,
            portfolio.PositionsMarketValue,
            portfolio.CashAvailable + portfolio.PositionsMarketValue));
    }

    [HttpGet("portfolio")]
    [EndpointName("GetTradingPortfolio")]
    [EndpointSummary("View portfolio valuation")]
    [EndpointDescription("Cash + positions valued using the latest stored quote/fill prices.")]
    public ActionResult<PortfolioResponse> GetPortfolio(int customerId)
    {
        var customer = customerService.GetCustomer(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var tradingAccount = customer.TradingAccount;
        var portfolio = portfolioService.GetPortfolio(
            tradingAccount.Id,
            tradingAccount.Currency);

        return Ok(new PortfolioResponse(
            portfolio.TradingAccountId,
            portfolio.Currency,
            portfolio.CashAvailable,
            portfolio.PositionsMarketValue,
            portfolio.CashAvailable + portfolio.PositionsMarketValue,
            portfolio.Positions
                .Select(p => new PositionResponse(
                    p.AssetSymbol,
                    p.Quantity,
                    p.LastPrice,
                    p.MarketValue,
                    p.PriceObservedUtc))
                .ToArray()));
    }

    [HttpGet("positions")]
    [EndpointName("GetTradingPositions")]
    [EndpointSummary("View positions")]
    [EndpointDescription("Lists asset holdings for the customer's trading account with last known prices.")]
    public ActionResult<IReadOnlyList<PositionResponse>> GetPositions(int customerId)
    {
        var customer = customerService.GetCustomer(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var portfolio = portfolioService.GetPortfolio(
            customer.TradingAccount.Id,
            customer.TradingAccount.Currency);

        var positions = portfolio.Positions
            .Select(p => new PositionResponse(
                p.AssetSymbol,
                p.Quantity,
                p.LastPrice,
                p.MarketValue,
                p.PriceObservedUtc))
            .ToArray();

        return Ok(positions);
    }

    [HttpGet("history")]
    [EndpointName("GetTradeHistory")]
    [EndpointSummary("View trade history")]
    [EndpointDescription("Returns buy/sell orders for the customer's trading account, newest first.")]
    public ActionResult<IReadOnlyList<TradeHistoryItemResponse>> GetHistory(int customerId)
    {
        var customer = customerService.GetCustomer(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var tradingAccount = customer.TradingAccount;
        var positionAccountId = customerDirectory.FindInvestmentAccountByTradingAccount(tradingAccount.Id)?.Id
            ?? tradingAccount.Id;

        var history = orderService.GetByAccount(positionAccountId)
            .Select(o => new TradeHistoryItemResponse(
                o.Id,
                o.AccountId,
                o.AssetSymbol,
                o.Side.ToString(),
                o.Quantity,
                o.LimitPrice,
                o.FillPrice,
                o.Provider,
                o.ExternalOrderId,
                o.Status.ToString(),
                o.CreatedUtc,
                o.SubmittedUtc,
                o.FilledUtc))
            .ToArray();

        return Ok(history);
    }

    [HttpPost("buys")]
    [EndpointName("BuyAsset")]
    [EndpointSummary("Buy asset")]
    [EndpointDescription(
        "Creates an investment instruction from parked trading cash and enqueues Investment → Asset buy (8001 → 8002 → 9001).")]
    public async Task<ActionResult<WorkflowAcceptedResponse>> Buy(
        int customerId,
        [FromBody] TradingOrderRequest body,
        CancellationToken ct)
    {
        var customer = customerService.GetCustomer(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var tradingAccountId = body.TradingAccountId is { } id && id != Guid.Empty
            ? id
            : customer.TradingAccount.Id;

        if (tradingAccountId != customer.TradingAccount.Id)
        {
            return BadRequest("Trading account does not belong to this customer.");
        }

        try
        {
            await workflowsService.EnqueueBuyAsync(new BuyWorkflowCommand
            {
                CustomerId = customerId,
                AccountId = tradingAccountId,
                AssetSymbol = body.AssetSymbol,
                Quantity = body.Quantity,
                Currency = body.Currency ?? customer.TradingAccount.Currency,
                IdempotencyKey = IdempotencyKeys.ForTrade("buy"),
                ExternalType = ExternalEntityType.TradingAccount
            }, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        return Accepted(WorkflowAcceptedResponse.RequestWillBeProcessed);
    }

    [HttpPost("sells")]
    [EndpointName("SellAsset")]
    [EndpointSummary("Sell asset")]
    [EndpointDescription(
        "Creates an investment instruction and enqueues Investment → Asset sell (8002 → 9002). Requires an existing investment position.")]
    public async Task<ActionResult<WorkflowAcceptedResponse>> Sell(
        int customerId,
        [FromBody] TradingOrderRequest body,
        CancellationToken ct)
    {
        var customer = customerService.GetCustomer(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var tradingAccountId = body.TradingAccountId is { } id && id != Guid.Empty
            ? id
            : customer.TradingAccount.Id;

        if (tradingAccountId != customer.TradingAccount.Id)
        {
            return BadRequest("Trading account does not belong to this customer.");
        }

        try
        {
            await workflowsService.EnqueueSellAsync(new SellWorkflowCommand
            {
                CustomerId = customerId,
                AccountId = tradingAccountId,
                AssetSymbol = body.AssetSymbol,
                Quantity = body.Quantity,
                Currency = body.Currency ?? customer.TradingAccount.Currency,
                IdempotencyKey = IdempotencyKeys.ForTrade("sell"),
                ExternalType = ExternalEntityType.TradingAccount
            }, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        return Accepted(WorkflowAcceptedResponse.RequestWillBeProcessed);
    }

    [HttpPost("transfer-to-customer")]
    [EndpointName("TransferFundsToCustomer")]
    [EndpointSummary("Transfer funds to customer")]
    [EndpointDescription("Moves parked funds from TradingAccount back to CustomerAccount (7003 → 6003).")]
    public async Task<ActionResult<WorkflowAcceptedResponse>> TransferToCustomer(
        int customerId,
        [FromBody] TradingTransferToCustomerHttpRequest body,
        CancellationToken ct)
    {
        var customer = customerService.GetCustomer(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var tradingAccountId = body.TradingAccountId is { } ta && ta != Guid.Empty
            ? ta
            : customer.TradingAccount.Id;
        var customerAccountId = body.CustomerAccountId is { } ca && ca != Guid.Empty
            ? ca
            : customer.CustomerAccount.Id;

        if (tradingAccountId != customer.TradingAccount.Id
            || customerAccountId != customer.CustomerAccount.Id)
        {
            return BadRequest("Account ids do not belong to this customer.");
        }

        await workflowsService.EnqueueTradingTransferToCustomerAsync(
            new TradingTransferToCustomerWorkflowCommand
            {
                CustomerId = customerId,
                TradingAccountId = tradingAccountId,
                CustomerAccountId = customerAccountId,
                Amount = body.Amount,
                Currency = body.Currency ?? customer.TradingAccount.Currency,
                IdempotencyKey = body.PaymentReference
            },
            ct);

        return Accepted(WorkflowAcceptedResponse.RequestWillBeProcessed);
    }
}
