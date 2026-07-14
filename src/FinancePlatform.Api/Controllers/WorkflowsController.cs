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

    [HttpPost("sells")]
    [EndpointName("EnqueueSell")]
    [EndpointSummary("Enqueue sell asset trigger")]
    [EndpointDescription("Creates a SellAsset trigger. Account must already hold the position.")]
    public async Task<ActionResult<WorkflowAcceptedResponse>> EnqueueSell(
        [FromBody] SellRequest body,
        CancellationToken ct)
    {
        await workflowsService.EnqueueSellAsync(new SellWorkflowCommand
        {
            AccountId = body.AccountId,
            AssetSymbol = body.AssetSymbol,
            Quantity = body.Quantity,
            CashAmount = body.CashAmount,
            Currency = body.Currency ?? "GBP",
            IdempotencyKey = body.PaymentReference,
            AllocationRequestId = body.AllocationRequestId
        }, ct);

        return Accepted(WorkflowAcceptedResponse.RequestWillBeProcessed);
    }
}
