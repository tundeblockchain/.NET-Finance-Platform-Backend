/*
  Procedure : dbo.DebitTradingAccount
  Purpose   : Debits Settled on a TradingAccount when available funds suffice. Idempotent via BrokerMutation.IdempotencyKey.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.DebitTradingAccount
    @IdempotencyKey NVARCHAR(200),
    @AccountId UNIQUEIDENTIFIER,
    @Amount DECIMAL(18,4),
    @TriggerId UNIQUEIDENTIFIER = NULL,
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Amount <= 0
    BEGIN
        THROW 50026, 'Debit amount must be positive.', 1;
    END

    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.BrokerMutation WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT *, CAST(1 AS BIT) AS AlreadyApplied FROM dbo.TradingAccount WHERE Id = @AccountId;
        COMMIT TRAN;
        RETURN;
    END

    IF NOT EXISTS (
        SELECT 1 FROM dbo.TradingAccount
        WHERE Id = @AccountId AND (Settled - Reserved) >= @Amount
    )
    BEGIN
        ROLLBACK TRAN;
        THROW 50027, 'Insufficient funds in trading account.', 1;
    END

    UPDATE dbo.TradingAccount
    SET Settled = Settled - @Amount,
        DateModified = SYSUTCDATETIME(),
        ChangedBy = @ChangedBy
    WHERE Id = @AccountId;

    INSERT INTO dbo.BrokerMutation (IdempotencyKey, TriggerId, CreatedUtc)
    VALUES (@IdempotencyKey, @TriggerId, SYSUTCDATETIME());

    COMMIT TRAN;

    SELECT *, CAST(0 AS BIT) AS AlreadyApplied FROM dbo.TradingAccount WHERE Id = @AccountId;
END
GO
