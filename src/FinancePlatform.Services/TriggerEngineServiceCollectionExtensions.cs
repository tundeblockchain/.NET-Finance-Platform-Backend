using FinancePlatform.Data;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Trading;
using FinancePlatform.Services.Triggers;
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

        services.AddSingleton<ICashService, InMemoryCashService>();
        services.AddSingleton<ITradingService, InMemoryTradingService>();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<TriggerHandlerRegistry>(sp =>
        {
            var registry = new TriggerHandlerRegistry();
            foreach (var handler in sp.GetServices<ITriggerHandler>())
            {
                registry.RegisterHandler(handler);
            }

            return registry;
        });

        services.AddSingleton<TriggerClaimService>();
        services.AddSingleton<TriggerRetryService>();
        services.AddSingleton<TriggerHeartbeatService>();
        services.AddSingleton<TriggerExecutionService>();

        return services;
    }

    /// <summary>
    /// Phase 2 helper — always uses the in-memory trigger store.
    /// </summary>
    public static IServiceCollection AddInMemoryTriggerEngine(this IServiceCollection services)
    {
        services.AddInMemoryPersistence();
        services.AddSingleton<ICashService, InMemoryCashService>();
        services.AddSingleton<ITradingService, InMemoryTradingService>();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<TriggerHandlerRegistry>(sp =>
        {
            var registry = new TriggerHandlerRegistry();
            foreach (var handler in sp.GetServices<ITriggerHandler>())
            {
                registry.RegisterHandler(handler);
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
