using FinancePlatform.Data.Triggers;
using FinancePlatform.Services.Allocation;
using FinancePlatform.Services.Asset;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Investment;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Positions;
using FinancePlatform.Services.Trade;
using FinancePlatform.Services.Triggers;
using FinancePlatform.UnitTests.Support;
using FinancePlatform.Worker.EventProcessors;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FinancePlatform.UnitTests.Triggers.Support;

internal sealed record TriggerExecutionTestHarness(
    InMemoryTriggerStore Store,
    TriggerEventProcessorRegistry Registry,
    InMemoryCashService Cash,
    ICustomerDirectory Directory,
    ICustomerService Customer,
    ITradeService Trading,
    IPositionService Positions,
    IOrderService Orders,
    TriggerExecutionService Execution)
{
    public static TriggerExecutionTestHarness Create(bool registerDefaults = true)
    {
        var store = new InMemoryTriggerStore();
        var registry = new TriggerEventProcessorRegistry();
        var cash = new InMemoryCashService();
        var ledger = new InMemoryLedgerService();
        var positions = new InMemoryPositionService();
        var orders = new InMemoryOrderService();
        var directory = new InMemoryCustomerDirectory();
        var allocation = new AllocationService();
        var trade = TradeServiceTestFactory.Create(cash, ledger, orders, positions, directory);
        var customer = new CustomerService(directory);
        var investment = new InvestmentService();
        var asset = new AssetService(trade, allocation);
        var cashComponent = new CashComponentService(cash, ledger);

        if (registerDefaults)
        {
            registry.Register(new CashEP(cashComponent));
            registry.Register(new CustomerEP(customer));
            registry.Register(new TradeEP(trade));
            registry.Register(new InvestmentEP(investment));
            registry.Register(new AssetEP(asset));
        }

        var retryOptions = Options.Create(new TriggerRetryOptions
        {
            BaseDelayMilliseconds = 10,
            MaxDelayMilliseconds = 100,
            JitterFactor = 0
        });

        var retry = new TriggerRetryService(
            store,
            retryOptions,
            TimeProvider.System,
            NullLogger<TriggerRetryService>.Instance);

        var execution = new TriggerExecutionService(
            store,
            registry,
            retry,
            NullLogger<TriggerExecutionService>.Instance);

        return new TriggerExecutionTestHarness(
            store, registry, cash, directory, customer, trade, positions, orders, execution);
    }
}
