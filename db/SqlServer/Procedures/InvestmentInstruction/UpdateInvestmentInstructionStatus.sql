/*
  Procedure : dbo.UpdateInvestmentInstructionStatus
  Purpose   : Updates InvestmentInstruction.Status and archives prior row.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.UpdateInvestmentInstructionStatus
    @Id UNIQUEIDENTIFIER,
    @Status INT,
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.InvestmentInstruction WHERE Id = @Id)
    BEGIN
        THROW 50047, 'Investment instruction was not found.', 1;
    END

    INSERT INTO dbo.InvestmentInstruction_a (
        Id, CustomerId, TradingAccountId, InvestmentAccountId, AssetSymbol, Quantity, CashAmount,
        Currency, Side, Status, OrderId, IdempotencyKey, CreatedUtc, DateModified, ChangedBy)
    SELECT Id, CustomerId, TradingAccountId, InvestmentAccountId, AssetSymbol, Quantity, CashAmount,
        Currency, Side, Status, OrderId, IdempotencyKey, CreatedUtc, DateModified, ChangedBy
    FROM dbo.InvestmentInstruction WHERE Id = @Id;

    UPDATE dbo.InvestmentInstruction
    SET Status = @Status,
        DateModified = SYSUTCDATETIME(),
        ChangedBy = @ChangedBy
    WHERE Id = @Id;

    SELECT * FROM dbo.InvestmentInstruction WHERE Id = @Id;
END
GO
