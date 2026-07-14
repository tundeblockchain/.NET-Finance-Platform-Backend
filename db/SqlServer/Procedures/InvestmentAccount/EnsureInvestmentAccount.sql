/*
  Procedure : dbo.EnsureInvestmentAccount
  Purpose   : Idempotently creates an InvestmentAccount for a TradingAccount if missing.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.EnsureInvestmentAccount
    @CustomerId INT,
    @TradingAccountId UNIQUEIDENTIFIER,
    @Currency NVARCHAR(3),
    @ChangedBy NVARCHAR(100) = N'system'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.InvestmentAccount WHERE TradingAccountId = @TradingAccountId)
    BEGIN
        SELECT * FROM dbo.InvestmentAccount WHERE TradingAccountId = @TradingAccountId;
        COMMIT TRAN;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.TradingAccount WHERE Id = @TradingAccountId AND CustomerId = @CustomerId)
    BEGIN
        ROLLBACK TRAN;
        THROW 50040, 'Trading account was not found for customer.', 1;
    END

    DECLARE @Id UNIQUEIDENTIFIER = NEWID();
    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    INSERT INTO dbo.InvestmentAccount (
        Id, CustomerId, TradingAccountId, Currency, Settled, Reserved, IsLocked,
        LockedByTriggerId, LockExpiresUtc, CreatedUtc, DateModified, ChangedBy)
    VALUES (
        @Id, @CustomerId, @TradingAccountId, UPPER(@Currency), 0, 0, 0,
        NULL, NULL, @Now, @Now, @ChangedBy);

    COMMIT TRAN;

    SELECT * FROM dbo.InvestmentAccount WHERE Id = @Id;
END
GO
