using FluentAssertions;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Triggers;

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

public class TriggerCodesTests
{
    [Theory]
    [InlineData(TriggerCodes.CustomerDistributeMoney, ComponentType.Customer, "Customer")]
    [InlineData(TriggerCodes.TradingReceiveMoney, ComponentType.Trading, "Trading")]
    [InlineData(TriggerCodes.InvestmentInvestMoney, ComponentType.Investment, "Investment")]
    [InlineData(TriggerCodes.AssetBuyAsset, ComponentType.AssetTrading, "AssetTrading")]
    public void GetOwningComponent_maps_architecture_ranges(int code, ComponentType expected, string rangeName)
    {
        TriggerCodes.GetOwningComponent(code).Should().Be(expected);
        TriggerCodes.GetRangeName(code).Should().Be(rangeName);
        TriggerCodes.IsInRange(code, expected).Should().BeTrue();
    }

    [Fact]
    public void Compensate_negates_absolute_code()
    {
        TriggerCodes.Compensate(TriggerCodes.BuyAsset).Should().Be(-2002);
        TriggerCodes.Compensate(-2002).Should().Be(-2002);
        TriggerCodes.IsCompensation(-2002).Should().BeTrue();
        TriggerCodes.IsAction(TriggerCodes.BuyAsset).Should().BeTrue();
    }

    [Fact]
    public void Unassigned_codes_have_no_component_owner()
    {
        TriggerCodes.GetOwningComponent(TriggerCodes.DepositCash).Should().BeNull();
        TriggerCodes.GetRangeName(TriggerCodes.DepositCash).Should().Be("Unassigned");
    }
}

public class TriggerStatusTransitionsTests
{
    [Theory]
    [InlineData(TriggerStatus.Pending, TriggerStatus.Claimed, true)]
    [InlineData(TriggerStatus.Claimed, TriggerStatus.Running, true)]
    [InlineData(TriggerStatus.Running, TriggerStatus.Completed, true)]
    [InlineData(TriggerStatus.Running, TriggerStatus.Retry, true)]
    [InlineData(TriggerStatus.Running, TriggerStatus.Failed, true)]
    [InlineData(TriggerStatus.Running, TriggerStatus.Compensation, true)]
    [InlineData(TriggerStatus.Retry, TriggerStatus.Pending, true)]
    [InlineData(TriggerStatus.Failed, TriggerStatus.Compensation, true)]
    [InlineData(TriggerStatus.Pending, TriggerStatus.Completed, false)]
    [InlineData(TriggerStatus.Completed, TriggerStatus.Running, false)]
    [InlineData(TriggerStatus.Claimed, TriggerStatus.Pending, false)]
    public void CanTransition_enforces_lifecycle(TriggerStatus from, TriggerStatus to, bool expected)
    {
        TriggerStatusTransitions.CanTransition(from, to).Should().Be(expected);
    }

    [Fact]
    public void EnsureCanTransition_throws_for_invalid_transition()
    {
        var act = () => TriggerStatusTransitions.EnsureCanTransition(
            TriggerStatus.Pending,
            TriggerStatus.Completed);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Pending*Completed*");
    }
}

public class TriggerHandlerRegistryTests
{
    [Fact]
    public void Register_component_allows_lookup_by_trigger_code()
    {
        var registry = new TriggerHandlerRegistry();
        var handler = new StubHandler(TriggerCodes.CustomerDistributeMoney);
        registry.Register(new StubComponent(ComponentType.Customer, "Customer", handler));

        var resolved = registry.GetRequiredHandler(TriggerCodes.CustomerDistributeMoney);

        resolved.Should().BeSameAs(handler);
        registry.RegisteredTriggerCodes.Should().Contain(TriggerCodes.CustomerDistributeMoney);
    }

    [Fact]
    public void Register_rejects_duplicate_trigger_codes()
    {
        var registry = new TriggerHandlerRegistry();
        var handler = new StubHandler(TriggerCodes.TradingReceiveMoney);
        registry.RegisterHandler(handler);

        var act = () => registry.RegisterHandler(new StubHandler(TriggerCodes.TradingReceiveMoney));

        act.Should().Throw<InvalidOperationException>().WithMessage("*already registered*");
    }

    [Fact]
    public void Register_rejects_handler_outside_component_range()
    {
        var registry = new TriggerHandlerRegistry();
        var handler = new StubHandler(TriggerCodes.AssetBuyAsset);

        var act = () => registry.Register(new StubComponent(ComponentType.Customer, "Customer", handler));

        act.Should().Throw<InvalidOperationException>().WithMessage("*outside the owned range*");
    }

    private sealed class StubHandler(int triggerCode) : ITriggerHandler
    {
        public int TriggerCode { get; } = triggerCode;

        public Task<TriggerHandlerResult> ExecuteAsync(
            TriggerContext context,
            string payloadJson,
            CancellationToken cancellationToken) =>
            Task.FromResult(TriggerHandlerResult.Success());
    }

    private sealed class StubComponent(
        ComponentType componentType,
        string name,
        params ITriggerHandler[] handlers) : IComponent
    {
        public ComponentType ComponentType { get; } = componentType;

        public string Name { get; } = name;

        public IReadOnlyCollection<int> OwnedTriggerCodes { get; } =
            handlers.Select(h => h.TriggerCode).ToArray();

        public IEnumerable<ITriggerHandler> GetHandlers() => handlers;
    }
}
