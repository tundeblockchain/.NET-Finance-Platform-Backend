namespace FinancePlatform.Services.Brokers.Alpaca;

public sealed class AlpacaBrokerOptions
{
    public const string SectionName = "Brokers:Alpaca";

    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// When true, uses paper trading endpoints (default).
    /// </summary>
    public bool Paper { get; set; } = true;

    public string TradingBaseUrl { get; set; } = "https://paper-api.alpaca.markets";

    public string DataBaseUrl { get; set; } = "https://data.alpaca.markets";

    /// <summary>
    /// Max polls while waiting for a paper/live order to fill.
    /// </summary>
    public int FillPollAttempts { get; set; } = 10;

    public int FillPollDelayMilliseconds { get; set; } = 250;
}
