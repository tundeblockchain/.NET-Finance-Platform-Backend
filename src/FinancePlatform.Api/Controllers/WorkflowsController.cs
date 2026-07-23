using FinancePlatform.Api.Contracts;
using FinancePlatform.Services.Workflows;
using Microsoft.AspNetCore.Mvc;

namespace FinancePlatform.Api.Controllers;

[ApiController]
[Route("api/workflows")]
[Tags("Workflows")]
public sealed class WorkflowsController(IWorkflowEnqueueService workflowsService) : ControllerBase
{
    [HttpPost("deposits")]
    [EndpointName("EnqueueDeposit")]
    [EndpointSummary("Enqueue legacy deposit → buy workflow")]
    [EndpointDescription("Creates a DepositCash (1001) root trigger. Prefer POST /api/customers/{id}/deposits for the customer account path.")]
    public async Task<ActionResult<WorkflowAcceptedResponse>> EnqueueDeposit(
        [FromBody] DepositRequest body,
        CancellationToken ct)
    {
        await workflowsService.EnqueueDepositAsync(new DepositWorkflowCommand
        {
            AccountId = body.AccountId,
            Amount = body.Amount,
            Currency = body.Currency ?? "GBP",
            AssetSymbol = body.AssetSymbol ?? "VWRL",
            Quantity = body.Quantity <= 0 ? 1m : body.Quantity,
            IdempotencyKey = body.PaymentReference
        }, ct);

        return Accepted(WorkflowAcceptedResponse.RequestWillBeProcessed);
    }

    [HttpPost("buys")]
    [EndpointName("EnqueueBuy")]
    [EndpointSummary("Enqueue buy via investment instruction")]
    [EndpointDescription("Creates an investment instruction and InvestmentReceiveMoney (8001) root trigger. Prefer POST /api/trading/customers/{id}/buys.")]
    public async Task<ActionResult<WorkflowAcceptedResponse>> EnqueueBuy(
        [FromBody] BuyRequest body,
        CancellationToken ct)
    {
        try
        {
            await workflowsService.EnqueueBuyAsync(new BuyWorkflowCommand
            {
                AccountId = body.AccountId,
                AssetSymbol = body.AssetSymbol,
                Quantity = body.Quantity,
                Currency = body.Currency ?? "GBP",
                IdempotencyKey = IdempotencyKeys.ForTrade("buy"),
                AllocationRequestId = body.AllocationRequestId
            }, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        return Accepted(WorkflowAcceptedResponse.RequestWillBeProcessed);
    }

    [HttpPost("sells")]
    [EndpointName("EnqueueSell")]
    [EndpointSummary("Enqueue sell via investment instruction")]
    [EndpointDescription("Creates an investment instruction and InvestmentInvestMoney (8002) root trigger. Prefer POST /api/trading/customers/{id}/sells.")]
    public async Task<ActionResult<WorkflowAcceptedResponse>> EnqueueSell(
        [FromBody] SellRequest body,
        CancellationToken ct)
    {
        try
        {
            await workflowsService.EnqueueSellAsync(new SellWorkflowCommand
            {
                AccountId = body.AccountId,
                AssetSymbol = body.AssetSymbol,
                Quantity = body.Quantity,
                Currency = body.Currency ?? "GBP",
                IdempotencyKey = IdempotencyKeys.ForTrade("sell"),
                AllocationRequestId = body.AllocationRequestId
            }, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        return Accepted(WorkflowAcceptedResponse.RequestWillBeProcessed);
    }
}
