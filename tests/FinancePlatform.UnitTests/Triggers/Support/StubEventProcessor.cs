using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.UnitTests.Triggers.Support;

internal sealed class StubEventProcessor(
    ComponentType componentType,
    string name,
    params int[] ownedCodes) : ITriggerEventProcessor
{
    private readonly HashSet<int> _codes = ownedCodes.Select(Math.Abs).ToHashSet();

    public string Name { get; } = name;

    public ComponentType? ComponentType { get; } = componentType;

    public bool CanProcess(int triggerCode) => _codes.Contains(Math.Abs(triggerCode));

    public Task<TriggerHandlerResult> ProcessAsync(
        TriggerContext context,
        int triggerCode,
        string payloadJson,
        ITriggerRaiser raiser,
        CancellationToken cancellationToken) =>
        Task.FromResult(TriggerHandlerResult.Success());
}
