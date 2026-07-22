using FinancePlatform.Services.Brokers.Alpaca;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancePlatform.Services.Brokers;

public static class BrokerServiceCollectionExtensions
{
    public const string ProviderKey = "Brokers:Provider";

    public static IServiceCollection AddBrokerTrading(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>(ProviderKey) ?? SimulatedBrokerTradingProvider.Name;

        services.Configure<AlpacaBrokerOptions>(configuration.GetSection(AlpacaBrokerOptions.SectionName));

        if (string.Equals(provider, AlpacaBrokerTradingProvider.Name, StringComparison.OrdinalIgnoreCase))
        {
            var alpaca = configuration.GetSection(AlpacaBrokerOptions.SectionName).Get<AlpacaBrokerOptions>()
                ?? new AlpacaBrokerOptions();

            if (string.IsNullOrWhiteSpace(alpaca.ApiKey) || string.IsNullOrWhiteSpace(alpaca.ApiSecret))
            {
                throw new InvalidOperationException(
                    "Brokers:Provider=Alpaca requires Brokers:Alpaca:ApiKey and Brokers:Alpaca:ApiSecret.");
            }

            var tradingBase = alpaca.Paper
                ? (string.IsNullOrWhiteSpace(alpaca.TradingBaseUrl)
                    ? "https://paper-api.alpaca.markets"
                    : alpaca.TradingBaseUrl)
                : (string.IsNullOrWhiteSpace(alpaca.TradingBaseUrl)
                    ? "https://api.alpaca.markets"
                    : alpaca.TradingBaseUrl);

            var dataBase = string.IsNullOrWhiteSpace(alpaca.DataBaseUrl)
                ? "https://data.alpaca.markets"
                : alpaca.DataBaseUrl;

            services.AddHttpClient(AlpacaBrokerTradingProvider.TradingClientName, (sp, client) =>
            {
                var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AlpacaBrokerOptions>>().Value;
                client.BaseAddress = new Uri(EnsureTrailingSlash(tradingBase));
                client.DefaultRequestHeaders.Remove("APCA-API-KEY-ID");
                client.DefaultRequestHeaders.Remove("APCA-API-SECRET-KEY");
                client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", opts.ApiKey);
                client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", opts.ApiSecret);
            });

            services.AddHttpClient(AlpacaBrokerTradingProvider.DataClientName, (sp, client) =>
            {
                var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AlpacaBrokerOptions>>().Value;
                client.BaseAddress = new Uri(EnsureTrailingSlash(dataBase));
                client.DefaultRequestHeaders.Remove("APCA-API-KEY-ID");
                client.DefaultRequestHeaders.Remove("APCA-API-SECRET-KEY");
                client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", opts.ApiKey);
                client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", opts.ApiSecret);
            });

            services.AddSingleton<IBrokerTradingProvider, AlpacaBrokerTradingProvider>();
            return services;
        }

        services.AddSingleton<IBrokerTradingProvider, SimulatedBrokerTradingProvider>();
        return services;
    }

    private static string EnsureTrailingSlash(string url) =>
        url.EndsWith('/') ? url : url + "/";
}
