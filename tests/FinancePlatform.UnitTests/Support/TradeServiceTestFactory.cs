using FinancePlatform.Services.Brokers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Positions;
using FinancePlatform.Services.Pricing;
using FinancePlatform.Services.Trade;

namespace FinancePlatform.UnitTests.Support;

internal static class TradeServiceTestFactory
{
    public static TradeService Create(
        ICashService cash,
        ILedgerService ledger,
        IOrderService orders,
        IPositionService positions,
        ICustomerDirectory? directory = null,
        IBrokerTradingProvider? broker = null,
        IAssetPriceService? prices = null) =>
        new(
            cash,
            ledger,
            orders,
            positions,
            directory ?? new InMemoryCustomerDirectory(),
            broker ?? new SimulatedBrokerTradingProvider(),
            prices ?? new InMemoryAssetPriceService());
}
