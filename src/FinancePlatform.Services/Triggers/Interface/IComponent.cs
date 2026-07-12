using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Triggers;

/// <summary>
/// A platform component that owns a set of trigger codes and their handlers.
/// </summary>
public interface IComponent
{
    ComponentType ComponentType { get; }

    string Name { get; }

    IReadOnlyCollection<int> OwnedTriggerCodes { get; }

    IEnumerable<ITriggerHandler> GetHandlers();
}
