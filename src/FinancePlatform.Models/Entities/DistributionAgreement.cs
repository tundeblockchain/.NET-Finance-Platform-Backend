using FinancePlatform.Models.Enums;

namespace FinancePlatform.Models.Entities;

public sealed class DistributionAgreement : IAuditableEntity
{
    public Guid Id { get; set; }

    public int CustomerId { get; set; }

    public ComponentType OwnerComponent { get; set; }

    public Guid OwnerAccountId { get; set; }

    public required string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
