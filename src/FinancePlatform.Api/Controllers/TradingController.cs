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

        var portfolio = portfolioService.GetPortfolio(
            customer.TradingAccount.Id,
            customer.TradingAccount.Currency);

        var cash = CustomerMapper.ToTradingAccountBalance(customer.TradingAccount);
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
            cash.Available + portfolio.PositionsMarketValue));
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

        var portfolio = portfolioService.GetPortfolio(
            customer.TradingAccount.Id,
            customer.TradingAccount.Currency);
        var cashAvailable = customer.TradingAccount.Available;

        return Ok(new PortfolioResponse(
            portfolio.TradingAccountId,
            portfolio.Currency,
            cashAvailable,
            portfolio.PositionsMarketValue,
            cashAvailable + portfolio.PositionsMarketValue,
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

        var history = orderService.GetByAccount(customer.TradingAccount.Id)
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
    [EndpointDescription("Enqueues a buy against the trading account. Requires parked trading cash.")]
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

        var trigger = await workflowsService.EnqueueBuyAsync(new BuyWorkflowCommand
        {
            AccountId = tradingAccountId,
            AssetSymbol = body.AssetSymbol,
            Quantity = body.Quantity,
            Currency = body.Currency ?? customer.TradingAccount.Currency,
            IdempotencyKey = IdempotencyKeys.ForTrade("buy"),
            ExternalType = ExternalEntityType.TradingAccount
        }, ct);

        return Accepted(
            $"/api/workflows/triggers/{trigger.Id}",
            new WorkflowAcceptedResponse(trigger.Id, trigger.RootWorkflowId, trigger.TriggerCode, trigger.QueueName));
    }

    [HttpPost("sells")]
    [EndpointName("SellAsset")]
    [EndpointSummary("Sell asset")]
    [EndpointDescription("Enqueues a sell against the trading account. Requires an existing position.")]
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

        var trigger = await workflowsService.EnqueueSellAsync(new SellWorkflowCommand
        {
            AccountId = tradingAccountId,
            AssetSymbol = body.AssetSymbol,
            Quantity = body.Quantity,
            Currency = body.Currency ?? customer.TradingAccount.Currency,
            IdempotencyKey = IdempotencyKeys.ForTrade("sell"),
            ExternalType = ExternalEntityType.TradingAccount
        }, ct);

        return Accepted(
            $"/api/workflows/triggers/{trigger.Id}",
            new WorkflowAcceptedResponse(trigger.Id, trigger.RootWorkflowId, trigger.TriggerCode, trigger.QueueName));
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

        var trigger = await workflowsService.EnqueueTradingTransferToCustomerAsync(
            new TradingTransferToCustomerWorkflowCommand
            {
                CustomerId = customerId,
                TradingAccountId = tradingAccountId,
                CustomerAccountId = customerAccountId,
                Amount = body.Amount,
                Currency = body.Currency ?? customer.TradingAccount.Currency,
                IdempotencyKey = body.IdempotencyKey,
                RootWorkflowId = body.RootWorkflowId
            },
            ct);

        return Accepted(
            $"/api/workflows/triggers/{trigger.Id}",
            new WorkflowAcceptedResponse(trigger.Id, trigger.RootWorkflowId, trigger.TriggerCode, trigger.QueueName));
    }
}
