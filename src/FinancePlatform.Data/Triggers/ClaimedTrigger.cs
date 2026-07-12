using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.Triggers;

public sealed class ClaimedTrigger
{
    public required SystemEventTrigger Trigger { get; init; }

    public required SystemEventWorking Working { get; init; }
}
