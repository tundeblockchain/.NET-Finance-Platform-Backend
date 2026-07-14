using FinancePlatform.Models.Enums;

namespace FinancePlatform.Models.Entities;

/// <summary>
/// Trading-created instruction to invest in an asset. Investment EP creates the order; Asset EP executes it.
/// </summary>
public sealed class InvestmentInstruction : IAuditableEntity
{
    public Guid Id { get; set; }

    public int CustomerId { get; set; }

    public Guid TradingAccountId { get; set; }

    public Guid InvestmentAccountId { get; set; }

    public required string AssetSymbol { get; set; }

    public decimal Quantity { get; set; }

    public decimal CashAmount { get; set; }

    public required string Currency { get; set; }

    public OrderSide Side { get; set; }

    public InvestmentInstructionStatus Status { get; set; } = InvestmentInstructionStatus.Pending;

    public Guid? OrderId { get; set; }

    public required string IdempotencyKey { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
