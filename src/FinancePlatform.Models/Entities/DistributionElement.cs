using FinancePlatform.Models.Enums;

namespace FinancePlatform.Models.Entities;

public sealed class DistributionElement : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid AgreementId { get; set; }

    public DistributionTargetType TargetType { get; set; }

    public Guid TargetAccountId { get; set; }

    /// <summary>
    /// Share of the distribute amount (1.0 = 100%).
    /// </summary>
    public decimal Percentage { get; set; } = 1m;

    public int Priority { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
