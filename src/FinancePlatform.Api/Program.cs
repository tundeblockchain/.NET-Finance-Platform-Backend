using FinancePlatform.Services;
using FinancePlatform.Services.Workflows;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();
builder.Services.AddTriggerEngine(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Finance Platform API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.MapGet("/", () => Results.Ok(new
{
    service = "FinancePlatform.Api",
    status = "ready",
    phase = 5,
    docs = "/scalar"
}))
.WithName("GetServiceInfo")
.WithTags("Meta")
.WithSummary("Service info")
.WithDescription("Returns basic service metadata including the current build phase.");

app.MapHealthChecks("/health");

var workflows = app.MapGroup("/api/workflows").WithTags("Workflows");

workflows.MapPost("/deposits", async (DepositRequest body, IWorkflowEnqueueService workflowsService, CancellationToken ct) =>
{
    var trigger = await workflowsService.EnqueueDepositAsync(new DepositWorkflowCommand
    {
        AccountId = body.AccountId,
        Amount = body.Amount,
        Currency = body.Currency ?? "GBP",
        AssetSymbol = body.AssetSymbol ?? "VWRL",
        Quantity = body.Quantity <= 0 ? 1m : body.Quantity,
        IdempotencyKey = body.IdempotencyKey,
        RootWorkflowId = body.RootWorkflowId
    }, ct);

    return Results.Accepted($"/api/workflows/triggers/{trigger.Id}", new WorkflowAcceptedResponse(trigger.Id, trigger.RootWorkflowId, trigger.TriggerCode, trigger.QueueName));
})
.WithName("EnqueueDeposit")
.WithSummary("Enqueue deposit → buy workflow")
.WithDescription("Creates a DepositCash root trigger. Worker processes deposit then enqueues buy.");

workflows.MapPost("/buys", async (BuyRequest body, IWorkflowEnqueueService workflowsService, CancellationToken ct) =>
{
    var trigger = await workflowsService.EnqueueBuyAsync(new BuyWorkflowCommand
    {
        AccountId = body.AccountId,
        AssetSymbol = body.AssetSymbol,
        Quantity = body.Quantity,
        CashAmount = body.CashAmount,
        Currency = body.Currency ?? "GBP",
        IdempotencyKey = body.IdempotencyKey,
        RootWorkflowId = body.RootWorkflowId,
        AllocationRequestId = body.AllocationRequestId
    }, ct);

    return Results.Accepted($"/api/workflows/triggers/{trigger.Id}", new WorkflowAcceptedResponse(trigger.Id, trigger.RootWorkflowId, trigger.TriggerCode, trigger.QueueName));
})
.WithName("EnqueueBuy")
.WithSummary("Enqueue buy asset trigger")
.WithDescription("Creates a BuyAsset trigger. Account must already have available cash.");

workflows.MapPost("/sells", async (SellRequest body, IWorkflowEnqueueService workflowsService, CancellationToken ct) =>
{
    var trigger = await workflowsService.EnqueueSellAsync(new SellWorkflowCommand
    {
        AccountId = body.AccountId,
        AssetSymbol = body.AssetSymbol,
        Quantity = body.Quantity,
        CashAmount = body.CashAmount,
        Currency = body.Currency ?? "GBP",
        IdempotencyKey = body.IdempotencyKey,
        RootWorkflowId = body.RootWorkflowId,
        AllocationRequestId = body.AllocationRequestId
    }, ct);

    return Results.Accepted($"/api/workflows/triggers/{trigger.Id}", new WorkflowAcceptedResponse(trigger.Id, trigger.RootWorkflowId, trigger.TriggerCode, trigger.QueueName));
})
.WithName("EnqueueSell")
.WithSummary("Enqueue sell asset trigger")
.WithDescription("Creates a SellAsset trigger. Account must already hold the position.");

workflows.MapPost("/allocations", async (AllocationRequest body, IWorkflowEnqueueService workflowsService, CancellationToken ct) =>
{
    var trigger = await workflowsService.EnqueueAllocationAsync(new AllocationWorkflowCommand
    {
        AccountId = body.AccountId,
        Amount = body.Amount,
        Currency = body.Currency ?? "GBP",
        AssetSymbol = body.AssetSymbol ?? "VWRL",
        Quantity = body.Quantity <= 0 ? 1m : body.Quantity,
        IdempotencyKey = body.IdempotencyKey,
        RootWorkflowId = body.RootWorkflowId,
        AllocationRequestId = body.AllocationRequestId
    }, ct);

    return Results.Accepted($"/api/workflows/triggers/{trigger.Id}", new WorkflowAcceptedResponse(trigger.Id, trigger.RootWorkflowId, trigger.TriggerCode, trigger.QueueName));
})
.WithName("EnqueueAllocation")
.WithSummary("Enqueue allocation chain")
.WithDescription("Starts Customer.DistributeMoney (6002) → … → Asset.BuyAsset (9001).");

app.Run();

public partial class Program;

internal sealed record DepositRequest(
    Guid AccountId,
    decimal Amount,
    string IdempotencyKey,
    string? Currency = null,
    string? AssetSymbol = null,
    decimal Quantity = 1m,
    Guid? RootWorkflowId = null);

internal sealed record BuyRequest(
    Guid AccountId,
    string AssetSymbol,
    decimal Quantity,
    decimal CashAmount,
    string IdempotencyKey,
    string? Currency = null,
    Guid? RootWorkflowId = null,
    Guid? AllocationRequestId = null);

internal sealed record SellRequest(
    Guid AccountId,
    string AssetSymbol,
    decimal Quantity,
    decimal CashAmount,
    string IdempotencyKey,
    string? Currency = null,
    Guid? RootWorkflowId = null,
    Guid? AllocationRequestId = null);

internal sealed record AllocationRequest(
    Guid AccountId,
    decimal Amount,
    string IdempotencyKey,
    string? Currency = null,
    string? AssetSymbol = null,
    decimal Quantity = 1m,
    Guid? RootWorkflowId = null,
    Guid? AllocationRequestId = null);

internal sealed record WorkflowAcceptedResponse(
    Guid TriggerId,
    Guid RootWorkflowId,
    int TriggerCode,
    string QueueName);
