using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Triggers;

/// <summary>
/// A platform component that owns a trigger code range and an event processor.
/// </summary>
public interface IComponent
{
    ComponentType ComponentType { get; }

    string Name { get; }

    ITriggerEventProcessor EventProcessor { get; }
}
