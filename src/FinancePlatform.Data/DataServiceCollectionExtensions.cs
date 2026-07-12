using FinancePlatform.Data.DataLayer;
using FinancePlatform.Data.Sql;
using FinancePlatform.Data.Triggers;
using Microsoft.Extensions.DependencyInjection;

namespace FinancePlatform.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
        services.AddSingleton<ITriggerStore, SqlTriggerStore>();

        services.AddSingleton<IAccountRepository, AccountRepository>();
        services.AddSingleton<IAllocationRequestRepository, AllocationRequestRepository>();
        services.AddSingleton<ICashBalanceRepository, CashBalanceRepository>();
        services.AddSingleton<ICashReservationRepository, CashReservationRepository>();
        services.AddSingleton<IPositionRepository, PositionRepository>();
        services.AddSingleton<IOrderRepository, OrderRepository>();
        services.AddSingleton<ILedgerEntryRepository, LedgerEntryRepository>();

        return services;
    }

    public static IServiceCollection AddInMemoryPersistence(this IServiceCollection services)
    {
        services.AddSingleton<ITriggerStore, InMemoryTriggerStore>();
        return services;
    }
}
