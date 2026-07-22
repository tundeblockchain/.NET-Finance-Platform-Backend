namespace FinancePlatform.Api;

internal static class IdempotencyKeys
{
    public static string ForTrade(string side) =>
        $"{side.ToLowerInvariant()}-{Guid.NewGuid():N}";
}
