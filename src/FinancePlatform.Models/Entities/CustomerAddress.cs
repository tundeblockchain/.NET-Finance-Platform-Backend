namespace FinancePlatform.Models.Entities;

public sealed class CustomerAddress : IAuditableEntity
{
    public Guid Id { get; set; }

    public int CustomerId { get; set; }

    public required string Line1 { get; set; }

    public string? Line2 { get; set; }

    public required string City { get; set; }

    public string? Region { get; set; }

    public required string PostalCode { get; set; }

    public required string Country { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
