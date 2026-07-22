using FinancePlatform.Models.Enums;

namespace FinancePlatform.Models.Triggers;

/// <summary>
/// Allowed trigger status transitions for the durable workflow engine.
/// </summary>
public static class TriggerStatusTransitions
{
    private static readonly Dictionary<TriggerStatus, HashSet<TriggerStatus>> Allowed = new()
    {
        [TriggerStatus.Pending] = [TriggerStatus.Claimed],
        [TriggerStatus.Claimed] = [TriggerStatus.Running],
        [TriggerStatus.Running] =
        [
            TriggerStatus.Completed,
            TriggerStatus.Retry,
            TriggerStatus.Failed,
            TriggerStatus.Compensation
        ],
        [TriggerStatus.Retry] = [TriggerStatus.Pending],
        [TriggerStatus.Failed] = [TriggerStatus.Compensation, TriggerStatus.Pending],
        [TriggerStatus.Completed] = [],
        [TriggerStatus.Compensation] = [TriggerStatus.Completed, TriggerStatus.Failed]
    };

    public static bool CanTransition(TriggerStatus from, TriggerStatus to)
    {
        return Allowed.TryGetValue(from, out var next) && next.Contains(to);
    }

    public static void EnsureCanTransition(TriggerStatus from, TriggerStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException(
                $"Invalid trigger status transition from {from} to {to}.");
        }
    }

    public static IReadOnlyCollection<TriggerStatus> GetAllowedTargets(TriggerStatus from)
    {
        return Allowed.TryGetValue(from, out var next)
            ? next
            : [];
    }
}
