using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Triggers;

public class TriggerCodesTests
{
    [Theory]
    [InlineData(TriggerCodes.CustomerReceiveMoney, ComponentType.Customer, "Customer")]
    [InlineData(TriggerCodes.TradingTransferToCustomer, ComponentType.Trading, "Trading")]
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
