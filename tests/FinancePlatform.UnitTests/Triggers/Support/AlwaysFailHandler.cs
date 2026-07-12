using FinancePlatform.Models.Dtos;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.UnitTests.Triggers.Support;

internal sealed class AlwaysFailHandler(int triggerCode) : ITriggerHandler
{
    public int TriggerCode { get; } = triggerCode;

    public Task<TriggerHandlerResult> ExecuteAsync(
        TriggerContext context,
        string payloadJson,
        CancellationToken cancellationToken) =>
        Task.FromResult(TriggerHandlerResult.Failure("forced failure"));
}
