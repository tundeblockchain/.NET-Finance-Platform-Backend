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
        services.AddSingleton<IAssetPriceRepository, AssetPriceRepository>();
        services.AddSingleton<ILedgerEntryRepository, LedgerEntryRepository>();

        services.AddSingleton<ICustomerRepository, CustomerRepository>();
        services.AddSingleton<ICustomerAddressRepository, CustomerAddressRepository>();
        services.AddSingleton<ICustomerAccountRepository, CustomerAccountRepository>();
        services.AddSingleton<ITradingAccountRepository, TradingAccountRepository>();
        services.AddSingleton<IDistributionAgreementRepository, DistributionAgreementRepository>();
        services.AddSingleton<IDistributionElementRepository, DistributionElementRepository>();
        services.AddSingleton<ICustomerProvisionRepository, CustomerProvisionRepository>();

        return services;
    }

    public static IServiceCollection AddInMemoryPersistence(this IServiceCollection services)
    {
        services.AddSingleton<ITriggerStore, InMemoryTriggerStore>();
        return services;
    }
}
