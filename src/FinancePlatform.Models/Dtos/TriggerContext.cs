using FinancePlatform.Models.Enums;
using FinancePlatform.Models.ValueObjects;

namespace FinancePlatform.Models.Dtos;

/// <summary>
/// Context carried on every trigger so workflows remain correlatable and idempotent.
/// </summary>
public sealed class TriggerContext
{
    public required Guid RootWorkflowId { get; init; }

    public required Guid CorrelationId { get; init; }

    public Guid? ParentTriggerId { get; init; }

    public Guid? SourceTriggerId { get; init; }

    public Guid? AllocationRequestId { get; init; }

    /// <summary>
    /// Optional business entity this trigger primarily operates on.
    /// Interpreted using <see cref="ExternalType"/>.
    /// </summary>
    public Guid? ExternalId { get; init; }

    /// <summary>
    /// Discriminator for <see cref="ExternalId"/> (Customer, Account, or Transfer).
    /// </summary>
    public ExternalEntityType? ExternalType { get; init; }

    public required string SourceComponent { get; init; }

    public required string TargetComponent { get; init; }

    public required IdempotencyKey IdempotencyKey { get; init; }

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (RootWorkflowId == Guid.Empty)
        {
            errors.Add($"{nameof(RootWorkflowId)} is required.");
        }

        if (CorrelationId == Guid.Empty)
        {
            errors.Add($"{nameof(CorrelationId)} is required.");
        }

        if (string.IsNullOrWhiteSpace(SourceComponent))
        {
            errors.Add($"{nameof(SourceComponent)} is required.");
        }

        if (string.IsNullOrWhiteSpace(TargetComponent))
        {
            errors.Add($"{nameof(TargetComponent)} is required.");
        }

        if (string.IsNullOrWhiteSpace(IdempotencyKey.Value))
        {
            errors.Add($"{nameof(IdempotencyKey)} is required.");
        }

        if (ExternalId.HasValue ^ ExternalType.HasValue)
        {
            errors.Add($"{nameof(ExternalId)} and {nameof(ExternalType)} must both be set or both be empty.");
        }

        if (ExternalId == Guid.Empty)
        {
            errors.Add($"{nameof(ExternalId)} cannot be an empty Guid.");
        }

        return errors;
    }

    public bool IsValid => Validate().Count == 0;

    public void EnsureValid()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Invalid trigger context: {string.Join(" ", errors)}");
        }
    }
}
