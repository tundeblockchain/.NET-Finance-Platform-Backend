using FinancePlatform.Data;
using FinancePlatform.Services.Allocation;
using FinancePlatform.Services.Asset;
using FinancePlatform.Services.Brokers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Investment;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Portfolio;
using FinancePlatform.Services.Positions;
using FinancePlatform.Services.Pricing;
using FinancePlatform.Services.Trade;
using FinancePlatform.Services.Triggers;
using FinancePlatform.Services.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancePlatform.Services;

public static class TriggerEngineServiceCollectionExtensions
{
    public const string PersistenceProviderKey = "Persistence:Provider";

    public static IServiceCollection AddTriggerEngine(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>(PersistenceProviderKey) ?? "InMemory";
        var connectionString = configuration.GetConnectionString("FinancePlatform");
        var useSqlServer = string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase);

        if (useSqlServer)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Persistence:Provider=SqlServer requires ConnectionStrings:FinancePlatform.");
            }

            services.AddSqlServerPersistence(connectionString);
        }
        else
        {
            services.AddInMemoryPersistence();
        }

        services.AddBrokerTrading(configuration);
        return AddTriggerEngineCore(services, useSqlServer);
    }

    /// <summary>
    /// Always uses the in-memory trigger store and customer directory.
    /// </summary>
    public static IServiceCollection AddInMemoryTriggerEngine(this IServiceCollection services)
    {
        services.AddInMemoryPersistence();
        services.AddSingleton<IBrokerTradingProvider, SimulatedBrokerTradingProvider>();
        return AddTriggerEngineCore(services, useSqlServer: false);
    }

    private static IServiceCollection AddTriggerEngineCore(IServiceCollection services, bool useSqlServer)
    {
        // Supporting / primitive services
        services.AddSingleton<ICashService, InMemoryCashService>();
        services.AddSingleton<ILedgerService, InMemoryLedgerService>();
        services.AddSingleton<IPositionService, InMemoryPositionService>();
        services.AddSingleton<IOrderService, InMemoryOrderService>();

        if (useSqlServer)
        {
            services.AddSingleton<IAssetPriceService, SqlAssetPriceService>();
            services.AddSingleton<ICustomerDirectory, SqlCustomerDirectory>();
        }
        else
        {
            services.AddSingleton<IAssetPriceService, InMemoryAssetPriceService>();
            services.AddSingleton<ICustomerDirectory, InMemoryCustomerDirectory>();
        }

        services.AddSingleton<IPortfolioService, PortfolioService>();

        // Main component services (EP → Service → Models)
        services.AddSingleton<IAllocationService, AllocationService>();
        services.AddSingleton<ICashComponentService, CashComponentService>();
        services.AddSingleton<ICustomerService, CustomerService>();
        services.AddSingleton<ITradeService, TradeService>();
        services.AddSingleton<IInvestmentService, InvestmentService>();
        services.AddSingleton<IAssetService, AssetService>();

        services.AddSingleton<IWorkflowEnqueueService, WorkflowEnqueueService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<WorkerHealthTracker>();
        services.AddSingleton<TriggerEventProcessorRegistry>(sp =>
        {
            var registry = new TriggerEventProcessorRegistry();
            foreach (var processor in sp.GetServices<ITriggerEventProcessor>())
            {
                registry.Register(processor);
            }

            return registry;
        });

        services.AddSingleton<TriggerClaimService>();
        services.AddSingleton<TriggerRetryService>();
        services.AddSingleton<TriggerHeartbeatService>();
        services.AddSingleton<TriggerRecoveryService>();
        services.AddSingleton<TriggerExecutionService>();

        return services;
    }
}
