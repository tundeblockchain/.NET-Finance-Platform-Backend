using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Triggers;

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
