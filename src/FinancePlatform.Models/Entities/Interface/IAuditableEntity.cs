namespace FinancePlatform.Models.Entities;

/// <summary>
/// Rows that support DateModified / ChangedBy auditing.
/// Archived models also copy these columns into *_a on each *_u update.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset DateModified { get; set; }

    string ChangedBy { get; set; }
}
