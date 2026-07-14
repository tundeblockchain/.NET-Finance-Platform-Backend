using FinancePlatform.Api.Contracts;
using FinancePlatform.Models.Enums;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Investment;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Positions;
using FinancePlatform.Services.Workflows;
using Microsoft.AspNetCore.Mvc;

namespace FinancePlatform.Api.Controllers;

[ApiController]
[Route("api/trading/customers/{customerId:int}")]
[Tags("Trading")]
public sealed class TradingController(
    ICustomerService customerService,
    ICustomerDirectory customerDirectory,
    IInvestmentInstructionStore instructionStore,
    IWorkflowEnqueueService workflowsService,
    IOrderService orderService,
    IPositionService positionService) : ControllerBase
{
    [HttpGet("funds")]
    [EndpointName("GetTradingFunds")]
    [EndpointSummary("View trading funds")]
    [EndpointDescription("Returns parked cash in the trading account plus open asset positions.")]
    public ActionResult<TradingFundsResponse> GetFunds(int customerId)
    {
        var customer = customerService.GetCustomer(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var tradingAccount = customer.TradingAccount;
        var pendingInstructions = instructionStore.GetPendingCashAmount(tradingAccount.Id);
        var available = customerDirectory.GetTradingAvailable(tradingAccount.Id, pendingInstructions);

        var positionAccountId = customerDirectory.FindInvestmentAccountByTradingAccount(tradingAccount.Id)?.Id
            ?? tradingAccount.Id;

        var positions = positionService.GetByAccount(positionAccountId)
            .Select(p => new PositionResponse(p.AssetSymbol, p.Quantity))
            .ToArray();

        return Ok(new TradingFundsResponse(
            CustomerMapper.ToTradingAccountBalance(tradingAccount, available),
            positions));
    }

    [HttpGet("positions")]
    [EndpointName("GetTradingPositions")]
    [EndpointSummary("View positions")]
    [EndpointDescription("Lists asset holdings for the customer's trading account.")]
    public ActionResult<IReadOnlyList<PositionResponse>> GetPositions(int customerId)
    {
        var customer = customerService.GetCustomer(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var tradingAccount = customer.TradingAccount;
        var positionAccountId = customerDirectory.FindInvestmentAccountByTradingAccount(tradingAccount.Id)?.Id
            ?? tradingAccount.Id;

        var positions = positionService.GetByAccount(positionAccountId)
            .Select(p => new PositionResponse(p.AssetSymbol, p.Quantity))
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
                o.Status.ToString(),
                o.CreatedUtc,
                o.SubmittedUtc))
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

        await workflowsService.EnqueueBuyAsync(new BuyWorkflowCommand
        {
            AccountId = tradingAccountId,
            AssetSymbol = body.AssetSymbol,
            Quantity = body.Quantity,
            CashAmount = body.CashAmount,
            Currency = body.Currency ?? customer.TradingAccount.Currency,
            IdempotencyKey = body.PaymentReference,
            ExternalType = ExternalEntityType.TradingAccount
        }, ct);

        return Accepted(WorkflowAcceptedResponse.RequestWillBeProcessed);
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

        await workflowsService.EnqueueSellAsync(new SellWorkflowCommand
        {
            AccountId = tradingAccountId,
            AssetSymbol = body.AssetSymbol,
            Quantity = body.Quantity,
            CashAmount = body.CashAmount,
            Currency = body.Currency ?? customer.TradingAccount.Currency,
            IdempotencyKey = body.PaymentReference,
            ExternalType = ExternalEntityType.TradingAccount
        }, ct);

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
