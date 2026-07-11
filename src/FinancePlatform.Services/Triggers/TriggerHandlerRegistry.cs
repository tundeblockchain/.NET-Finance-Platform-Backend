using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;

namespace FinancePlatform.Services.Triggers;

/// <summary>
/// In-memory registration of components and handlers by trigger code.
/// </summary>
public sealed class TriggerHandlerRegistry
{
    private readonly Dictionary<int, ITriggerHandler> _handlers = new();
    private readonly Dictionary<ComponentType, IComponent> _components = new();

    public void Register(IComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        if (!_components.TryAdd(component.ComponentType, component))
        {
            throw new InvalidOperationException(
                $"Component '{component.Name}' ({component.ComponentType}) is already registered.");
        }

        foreach (var handler in component.GetHandlers())
        {
            RegisterHandler(component, handler);
        }
    }

    public void RegisterHandler(ITriggerHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        RegisterHandler(component: null, handler);
    }

    private void RegisterHandler(IComponent? component, ITriggerHandler handler)
    {
        if (handler.TriggerCode == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(handler), "Trigger code cannot be zero.");
        }

        if (component is not null && !TriggerCodes.IsInRange(handler.TriggerCode, component.ComponentType))
        {
            throw new InvalidOperationException(
                $"Handler code {handler.TriggerCode} is outside the owned range for component '{component.Name}'.");
        }

        if (!_handlers.TryAdd(handler.TriggerCode, handler))
        {
            throw new InvalidOperationException(
                $"Trigger code {handler.TriggerCode} is already registered.");
        }
    }

    public bool TryGetHandler(int triggerCode, out ITriggerHandler? handler) =>
        _handlers.TryGetValue(triggerCode, out handler);

    public ITriggerHandler GetRequiredHandler(int triggerCode)
    {
        if (!TryGetHandler(triggerCode, out var handler) || handler is null)
        {
            throw new KeyNotFoundException($"No handler registered for trigger code {triggerCode}.");
        }

        return handler;
    }

    public IReadOnlyCollection<int> RegisteredTriggerCodes => _handlers.Keys.OrderBy(x => x).ToArray();

    public IReadOnlyCollection<IComponent> RegisteredComponents => _components.Values.ToArray();
}
