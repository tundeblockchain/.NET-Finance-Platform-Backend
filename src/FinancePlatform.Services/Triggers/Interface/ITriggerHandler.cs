using FinancePlatform.Models.Dtos;

namespace FinancePlatform.Services.Triggers;

public interface ITriggerHandler
{
    int TriggerCode { get; }

    Task<TriggerHandlerResult> ExecuteAsync(TriggerContext context, string payloadJson, CancellationToken cancellationToken);
}
