using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.Triggers;

/// <summary>
/// A trigger whose expired working lease was cleared and requeued as Pending.
/// </summary>
public sealed class RecoveredTrigger
{
    public required SystemEventTrigger Trigger { get; init; }

    public required string PreviousWorkerInstanceId { get; init; }

    public required DateTimeOffset PreviousLeaseExpiresUtc { get; init; }
}
