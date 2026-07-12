using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Worker.Handlers;

public sealed class ReverseBuyAssetHandler : ITriggerHandler
{
    public int TriggerCode => TriggerCodes.Compensate(TriggerCodes.BuyAsset);

    public Task<TriggerHandlerResult> ExecuteAsync(
        TriggerContext context,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(TriggerHandlerResult.Success(resultJson: """{"status":"buy-reversed"}"""));
    }
}
