namespace FinancePlatform.Services.Positions;

public sealed record PositionHolding(Guid AccountId, string AssetSymbol, decimal Quantity);
