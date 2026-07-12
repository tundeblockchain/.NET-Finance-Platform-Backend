using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Triggers;

public class TriggerContextValidationTests
{
    [Fact]
    public void Validate_returns_no_errors_for_complete_context()
    {
        var context = CreateValidContext();

        context.Validate().Should().BeEmpty();
        context.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_requires_root_workflow_correlation_components_and_idempotency_key()
    {
        var context = new TriggerContext
        {
            TriggerId = Guid.NewGuid(),
            RootWorkflowId = Guid.Empty,
            CorrelationId = Guid.Empty,
            SourceComponent = " ",
            TargetComponent = "",
            IdempotencyKey = new("present")
        };

        var errors = context.Validate();

        errors.Should().Contain(e => e.Contains(nameof(TriggerContext.RootWorkflowId)));
        errors.Should().Contain(e => e.Contains(nameof(TriggerContext.CorrelationId)));
        errors.Should().Contain(e => e.Contains(nameof(TriggerContext.SourceComponent)));
        errors.Should().Contain(e => e.Contains(nameof(TriggerContext.TargetComponent)));
    }

    [Fact]
    public void Validate_requires_external_id_and_type_together()
    {
        var context = new TriggerContext
        {
            TriggerId = Guid.NewGuid(),
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Customer",
            TargetComponent = "Trading",
            IdempotencyKey = new("key-1"),
            ExternalId = Guid.NewGuid()
        };

        context.Validate()
            .Should().Contain(e => e.Contains(nameof(TriggerContext.ExternalId))
                && e.Contains(nameof(TriggerContext.ExternalType)));
    }

    [Fact]
    public void Validate_accepts_matching_external_id_and_type()
    {
        var context = new TriggerContext
        {
            TriggerId = Guid.NewGuid(),
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Customer",
            TargetComponent = "Trading",
            IdempotencyKey = new("key-1"),
            ExternalId = Guid.NewGuid(),
            ExternalType = ExternalEntityType.Account
        };

        context.Validate().Should().BeEmpty();
    }

    [Fact]
    public void IdempotencyKey_rejects_blank_values()
    {
        var act = () => new FinancePlatform.Models.ValueObjects.IdempotencyKey("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnsureValid_throws_when_invalid()
    {
        var context = new TriggerContext
        {
            TriggerId = Guid.NewGuid(),
            RootWorkflowId = Guid.Empty,
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Customer",
            TargetComponent = "Trading",
            IdempotencyKey = new("key-1")
        };

        var act = context.EnsureValid;

        act.Should().Throw<ArgumentException>().WithMessage("*RootWorkflowId*");
    }

    private static TriggerContext CreateValidContext() => new()
    {
        TriggerId = Guid.NewGuid(),
        RootWorkflowId = Guid.NewGuid(),
        CorrelationId = Guid.NewGuid(),
        SourceComponent = "Customer",
        TargetComponent = "Trading",
        IdempotencyKey = new("alloc-1:6002")
    };
}
