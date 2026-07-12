using FinancePlatform.Data.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Trading;
using FinancePlatform.Services.Triggers;
using FinancePlatform.Worker.Handlers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FinancePlatform.UnitTests.Triggers.Support;

internal sealed record TriggerExecutionTestHarness(
    InMemoryTriggerStore Store,
    TriggerHandlerRegistry Registry,
    InMemoryCashService Cash,
    InMemoryTradingService Trading,
    TriggerExecutionService Execution)
{
    public static TriggerExecutionTestHarness Create(bool registerDeposit = true)
    {
        var store = new InMemoryTriggerStore();
        var registry = new TriggerHandlerRegistry();
        var cash = new InMemoryCashService();
        var ledger = new InMemoryLedgerService();
        var trading = new InMemoryTradingService();

        if (registerDeposit)
        {
            registry.RegisterHandler(new DepositCashHandler(cash, ledger));
            registry.RegisterHandler(new BuyAssetHandler(trading));
            registry.RegisterHandler(new ReverseBuyAssetHandler());
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

        return new TriggerExecutionTestHarness(store, registry, cash, trading, execution);
    }
}
