using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Cash;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Triggers;
using Microsoft.Extensions.Options;

namespace FinancePlatform.Worker;

/// <summary>
/// Seeds a sample deposit → buy workflow once so the console worker demonstrates end-to-end processing.
/// </summary>
public sealed class SampleWorkflowHostedService(
    IOptions<BrokerOptions> options,
    TriggerClaimService claimService,
    ILogger<SampleWorkflowHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.SeedSampleWorkflowOnStartup)
        {
            return;
        }

        // Allow queue workers to start first.
        await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);

        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var rootWorkflowId = Guid.NewGuid();
        var payload = new DepositCashRequest
        {
            Amount = 1000m,
            Currency = "GBP",
            AssetSymbol = "VWRL",
            Quantity = 5m
        };

        var trigger = await claimService.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = "Cash",
            PayloadJson = JsonSerializer.Serialize(payload),
            RootWorkflowId = rootWorkflowId,
            CorrelationId = rootWorkflowId,
            ExternalId = accountId,
            ExternalType = ExternalEntityType.Account,
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = $"sample-deposit:{rootWorkflowId:N}"
        }, stoppingToken);

        logger.LogDebug(
            "Seeded sample workflow {RootWorkflowId} with deposit trigger {TriggerId}",
            rootWorkflowId,
            trigger.Id);
    }
}
