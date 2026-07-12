using FinancePlatform.Models.Enums;

namespace FinancePlatform.Models.Triggers;

/// <summary>
/// Trigger code ranges and named codes for component workflows.
/// Positive codes are actions; negation yields the compensating reverse.
/// </summary>
public static class TriggerCodes
{
    public const int CustomerMin = 6000;
    public const int CustomerMax = 6999;
    public const int TradingMin = 7000;
    public const int TradingMax = 7999;
    public const int InvestmentMin = 8000;
    public const int InvestmentMax = 8999;
    public const int AssetTradingMin = 9000;
    public const int AssetTradingMax = 9999;

    // Customer component
    public const int CustomerDepositMoney = 6001;
    public const int CustomerDistributeMoney = 6002;

    // Trading component
    public const int TradingReceiveMoney = 7001;
    public const int TradingDistributeMoney = 7002;
    public const int InvestmentReceiveMoney = 8001;
    public const int InvestmentInvestMoney = 8002;
    public const int AssetBuyAsset = 9001;
    public const int AssetSellAsset = 9002;

    // Classic cash/trading examples
    public const int DepositCash = 1001;
    public const int BuyAsset = 2002;
    public const int SellAsset = 2003;

    public static int Compensate(int triggerCode)
    {
        if (triggerCode == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(triggerCode), "Trigger code cannot be zero.");
        }

        return -Math.Abs(triggerCode);
    }

    public static int Absolute(int triggerCode) => Math.Abs(triggerCode);

    public static bool IsCompensation(int triggerCode) => triggerCode < 0;

    public static bool IsAction(int triggerCode) => triggerCode > 0;

    public static ComponentType? GetOwningComponent(int triggerCode)
    {
        var code = Absolute(triggerCode);

        return code switch
        {
            >= CustomerMin and <= CustomerMax => ComponentType.Customer,
            >= TradingMin and <= TradingMax => ComponentType.Trading,
            >= InvestmentMin and <= InvestmentMax => ComponentType.Investment,
            >= AssetTradingMin and <= AssetTradingMax => ComponentType.AssetTrading,
            _ => null
        };
    }

    public static bool IsInRange(int triggerCode, ComponentType component)
    {
        var code = Absolute(triggerCode);

        return component switch
        {
            ComponentType.Customer => code is >= CustomerMin and <= CustomerMax,
            ComponentType.Trading => code is >= TradingMin and <= TradingMax,
            ComponentType.Investment => code is >= InvestmentMin and <= InvestmentMax,
            ComponentType.AssetTrading => code is >= AssetTradingMin and <= AssetTradingMax,
            _ => false
        };
    }

    public static string GetRangeName(int triggerCode)
    {
        return GetOwningComponent(triggerCode) switch
        {
            ComponentType.Customer => "Customer",
            ComponentType.Trading => "Trading",
            ComponentType.Investment => "Investment",
            ComponentType.AssetTrading => "AssetTrading",
            _ => "Unassigned"
        };
    }
}
