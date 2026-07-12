using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;

namespace FinancePlatform.Services.Triggers;

/// <summary>
/// Resolves trigger codes to the owning component event processor.
/// </summary>
public sealed class TriggerEventProcessorRegistry
{
    private readonly List<ITriggerEventProcessor> _processors = [];
    private readonly Dictionary<ComponentType, ITriggerEventProcessor> _byComponent = new();

    public void Register(ITriggerEventProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(processor);

        if (processor.ComponentType is { } componentType
            && !_byComponent.TryAdd(componentType, processor))
        {
            throw new InvalidOperationException(
                $"Component '{processor.Name}' ({componentType}) is already registered.");
        }

        _processors.Add(processor);
    }

    public bool TryGetProcessor(int triggerCode, out ITriggerEventProcessor? processor)
    {
        var matches = _processors.Where(p => p.CanProcess(triggerCode)).ToArray();
        if (matches.Length == 0)
        {
            processor = null;
            return false;
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Trigger code {triggerCode} is claimed by multiple event processors: {string.Join(", ", matches.Select(m => m.Name))}.");
        }

        processor = matches[0];
        return true;
    }

    public ITriggerEventProcessor GetRequiredProcessor(int triggerCode)
    {
        if (!TryGetProcessor(triggerCode, out var processor) || processor is null)
        {
            throw new KeyNotFoundException($"No event processor registered for trigger code {triggerCode}.");
        }

        return processor;
    }

    public IReadOnlyCollection<ITriggerEventProcessor> RegisteredProcessors => _processors.ToArray();
}
