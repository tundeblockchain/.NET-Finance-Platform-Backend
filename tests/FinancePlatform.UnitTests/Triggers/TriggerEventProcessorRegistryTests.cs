using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Triggers;
using FinancePlatform.UnitTests.Triggers.Support;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Triggers;

public class TriggerEventProcessorRegistryTests
{
    [Fact]
    public void Register_allows_lookup_by_trigger_code()
    {
        var registry = new TriggerEventProcessorRegistry();
        var processor = new StubEventProcessor(
            ComponentType.Customer,
            "CustomerEP",
            TriggerCodes.CustomerDistributeMoney);
        registry.Register(processor);

        registry.GetRequiredProcessor(TriggerCodes.CustomerDistributeMoney).Should().BeSameAs(processor);
    }

    [Fact]
    public void Register_rejects_duplicate_component_types()
    {
        var registry = new TriggerEventProcessorRegistry();
        registry.Register(new StubEventProcessor(ComponentType.Customer, "CustomerEP", TriggerCodes.CustomerDistributeMoney));

        var act = () => registry.Register(
            new StubEventProcessor(ComponentType.Customer, "CustomerEP2", TriggerCodes.CustomerDistributeMoney + 1));

        act.Should().Throw<InvalidOperationException>().WithMessage("*already registered*");
    }

    [Fact]
    public void GetRequiredProcessor_throws_when_missing()
    {
        var registry = new TriggerEventProcessorRegistry();

        var act = () => registry.GetRequiredProcessor(TriggerCodes.AssetBuyAsset);

        act.Should().Throw<KeyNotFoundException>();
    }
}
