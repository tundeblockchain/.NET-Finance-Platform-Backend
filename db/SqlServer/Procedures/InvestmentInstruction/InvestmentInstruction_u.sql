/*
  Procedure : dbo.InvestmentInstruction_u
  Purpose   : Upserts an InvestmentInstruction. On update, archives into InvestmentInstruction_a.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.InvestmentInstruction_u
    @Id UNIQUEIDENTIFIER,
    @CustomerId INT,
    @TradingAccountId UNIQUEIDENTIFIER,
    @InvestmentAccountId UNIQUEIDENTIFIER,
    @AssetSymbol NVARCHAR(32),
    @Quantity DECIMAL(18,8),
    @CashAmount DECIMAL(18,4),
    @Currency NVARCHAR(3),
    @Side INT,
    @Status INT,
    @OrderId UNIQUEIDENTIFIER = NULL,
    @IdempotencyKey NVARCHAR(200),
    @CreatedUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.InvestmentInstruction WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.InvestmentInstruction_a (
            Id, CustomerId, TradingAccountId, InvestmentAccountId, AssetSymbol, Quantity, CashAmount,
            Currency, Side, Status, OrderId, IdempotencyKey, CreatedUtc, DateModified, ChangedBy)
        SELECT Id, CustomerId, TradingAccountId, InvestmentAccountId, AssetSymbol, Quantity, CashAmount,
            Currency, Side, Status, OrderId, IdempotencyKey, CreatedUtc, DateModified, ChangedBy
        FROM dbo.InvestmentInstruction WHERE Id = @Id;

        UPDATE dbo.InvestmentInstruction
        SET CustomerId = @CustomerId,
            TradingAccountId = @TradingAccountId,
            InvestmentAccountId = @InvestmentAccountId,
            AssetSymbol = @AssetSymbol,
            Quantity = @Quantity,
            CashAmount = @CashAmount,
            Currency = @Currency,
            Side = @Side,
            Status = @Status,
            OrderId = @OrderId,
            IdempotencyKey = @IdempotencyKey,
            CreatedUtc = @CreatedUtc,
            DateModified = SYSUTCDATETIME(),
            ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.InvestmentInstruction (
            Id, CustomerId, TradingAccountId, InvestmentAccountId, AssetSymbol, Quantity, CashAmount,
            Currency, Side, Status, OrderId, IdempotencyKey, CreatedUtc, DateModified, ChangedBy)
        VALUES (
            @Id, @CustomerId, @TradingAccountId, @InvestmentAccountId, @AssetSymbol, @Quantity, @CashAmount,
            @Currency, @Side, @Status, @OrderId, @IdempotencyKey, @CreatedUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.InvestmentInstruction WHERE Id = @Id;
END
GO
