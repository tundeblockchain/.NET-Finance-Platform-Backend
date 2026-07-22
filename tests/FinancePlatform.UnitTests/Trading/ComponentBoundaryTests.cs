using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Positions;
using FinancePlatform.Services.Trade;
using FinancePlatform.UnitTests.Support;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Trading;

public class ComponentBoundaryTests
{
    [Fact]
    public void Trigger_ranges_enforce_component_ownership()
    {
        TriggerCodes.GetOwningComponent(TriggerCodes.AssetBuyAsset).Should().Be(ComponentType.AssetTrading);
        TriggerCodes.GetOwningComponent(TriggerCodes.CustomerDistributeMoney).Should().Be(ComponentType.Customer);
        TriggerCodes.IsInRange(TriggerCodes.AssetBuyAsset, ComponentType.Customer).Should().BeFalse();
        TriggerCodes.IsInRange(TriggerCodes.CustomerDistributeMoney, ComponentType.AssetTrading).Should().BeFalse();
    }

    [Fact]
    public void Trade_service_reads_positions_from_shared_position_service()
    {
        var cash = new InMemoryCashService();
        var ledger = new InMemoryLedgerService();
        var positions = new InMemoryPositionService();
        var orders = new InMemoryOrderService();
        var trade = TradeServiceTestFactory.Create(cash, ledger, orders, positions);
        var accountId = Guid.NewGuid();

        positions.TryApplyBuy("p1", accountId, "VWRL", 5m);

        trade.GetPosition(accountId, "VWRL").Should().Be(5m);
        positions.GetQuantity(accountId, "VWRL").Should().Be(5m);
    }
}
