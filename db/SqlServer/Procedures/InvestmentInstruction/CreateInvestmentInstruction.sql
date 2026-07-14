/*
  Procedure : dbo.CreateInvestmentInstruction
  Purpose   : Idempotently creates an InvestmentInstruction by IdempotencyKey.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.CreateInvestmentInstruction
    @Id UNIQUEIDENTIFIER,
    @CustomerId INT,
    @TradingAccountId UNIQUEIDENTIFIER,
    @InvestmentAccountId UNIQUEIDENTIFIER,
    @AssetSymbol NVARCHAR(32),
    @Quantity DECIMAL(18,8),
    @CashAmount DECIMAL(18,4),
    @Currency NVARCHAR(3),
    @Side INT,
    @Status INT = 0,
    @IdempotencyKey NVARCHAR(200),
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.InvestmentInstruction WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT *, CAST(1 AS BIT) AS AlreadyApplied
        FROM dbo.InvestmentInstruction
        WHERE IdempotencyKey = @IdempotencyKey;
        COMMIT TRAN;
        RETURN;
    END

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    INSERT INTO dbo.InvestmentInstruction (
        Id, CustomerId, TradingAccountId, InvestmentAccountId, AssetSymbol, Quantity, CashAmount,
        Currency, Side, Status, OrderId, IdempotencyKey, CreatedUtc, DateModified, ChangedBy)
    VALUES (
        @Id, @CustomerId, @TradingAccountId, @InvestmentAccountId, @AssetSymbol, @Quantity, @CashAmount,
        @Currency, @Side, @Status, NULL, @IdempotencyKey, @Now, @Now, @ChangedBy);

    COMMIT TRAN;

    SELECT *, CAST(0 AS BIT) AS AlreadyApplied
    FROM dbo.InvestmentInstruction
    WHERE Id = @Id;
END
GO
