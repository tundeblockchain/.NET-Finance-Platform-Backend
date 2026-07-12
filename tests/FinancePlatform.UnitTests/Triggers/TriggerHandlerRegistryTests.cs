using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Triggers;
using FinancePlatform.UnitTests.Triggers.Support;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Triggers;

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
}
