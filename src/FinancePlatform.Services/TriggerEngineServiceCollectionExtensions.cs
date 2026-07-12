using FinancePlatform.Data;
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

        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
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

        return AddTriggerEngineCore(services);
    }

    /// <summary>
    /// Always uses the in-memory trigger store.
    /// </summary>
    public static IServiceCollection AddInMemoryTriggerEngine(this IServiceCollection services)
    {
        services.AddInMemoryPersistence();
        return AddTriggerEngineCore(services);
    }

    private static IServiceCollection AddTriggerEngineCore(IServiceCollection services)
    {
        // Supporting / primitive services
        services.AddSingleton<ICashService, InMemoryCashService>();
        services.AddSingleton<ILedgerService, InMemoryLedgerService>();
        services.AddSingleton<IPositionService, InMemoryPositionService>();
        services.AddSingleton<IOrderService, InMemoryOrderService>();

        // Main component services (EP → Service → Models)
        services.AddSingleton<IAllocationService, AllocationService>();
        services.AddSingleton<ICashComponentService, CashComponentService>();
        services.AddSingleton<ICustomerService, CustomerService>();
        services.AddSingleton<ITradeService, TradeService>();
        services.AddSingleton<IInvestmentService, InvestmentService>();
        services.AddSingleton<IAssetService, AssetService>();

        services.AddSingleton<IWorkflowEnqueueService, WorkflowEnqueueService>();
        services.AddSingleton(TimeProvider.System);

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
        services.AddSingleton<TriggerExecutionService>();

        return services;
    }
}
