/*
  Procedure : dbo.DebitInvestmentAccount
  Purpose   : Debits Settled on an InvestmentAccount when available funds suffice. Idempotent via BrokerMutation.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.DebitInvestmentAccount
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
        THROW 50043, 'Debit amount must be positive.', 1;
    END

    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.BrokerMutation WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT *, CAST(1 AS BIT) AS AlreadyApplied FROM dbo.InvestmentAccount WHERE Id = @AccountId;
        COMMIT TRAN;
        RETURN;
    END

    IF NOT EXISTS (
        SELECT 1 FROM dbo.InvestmentAccount
        WHERE Id = @AccountId AND (Settled - Reserved) >= @Amount
    )
    BEGIN
        ROLLBACK TRAN;
        THROW 50044, 'Insufficient funds in investment account.', 1;
    END

    UPDATE dbo.InvestmentAccount
    SET Settled = Settled - @Amount,
        DateModified = SYSUTCDATETIME(),
        ChangedBy = @ChangedBy
    WHERE Id = @AccountId;

    INSERT INTO dbo.BrokerMutation (IdempotencyKey, TriggerId, CreatedUtc)
    VALUES (@IdempotencyKey, @TriggerId, SYSUTCDATETIME());

    COMMIT TRAN;

    SELECT *, CAST(0 AS BIT) AS AlreadyApplied FROM dbo.InvestmentAccount WHERE Id = @AccountId;
END
GO
