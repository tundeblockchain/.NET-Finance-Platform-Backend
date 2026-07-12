using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.UnitTests.Triggers.Support;

internal sealed class StubComponent(
    ComponentType componentType,
    string name,
    params ITriggerHandler[] handlers) : IComponent
{
    public ComponentType ComponentType { get; } = componentType;

    public string Name { get; } = name;

    public IReadOnlyCollection<int> OwnedTriggerCodes { get; } =
        handlers.Select(h => h.TriggerCode).ToArray();

    public IEnumerable<ITriggerHandler> GetHandlers() => handlers;
}
