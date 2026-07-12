namespace FinancePlatform.Models.Entities;

public sealed class Customer : IAuditableEntity
{
    public int Id { get; set; }

    public required string Email { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
