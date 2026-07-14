/*
  Procedure : dbo.CreditInvestmentAccount
  Purpose   : Credits Settled on an InvestmentAccount. Idempotent via BrokerMutation.IdempotencyKey.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.CreditInvestmentAccount
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
        THROW 50041, 'Credit amount must be positive.', 1;
    END

    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.BrokerMutation WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT *, CAST(1 AS BIT) AS AlreadyApplied FROM dbo.InvestmentAccount WHERE Id = @AccountId;
        COMMIT TRAN;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.InvestmentAccount WHERE Id = @AccountId)
    BEGIN
        ROLLBACK TRAN;
        THROW 50042, 'Investment account was not found.', 1;
    END

    UPDATE dbo.InvestmentAccount
    SET Settled = Settled + @Amount,
        DateModified = SYSUTCDATETIME(),
        ChangedBy = @ChangedBy
    WHERE Id = @AccountId;

    INSERT INTO dbo.BrokerMutation (IdempotencyKey, TriggerId, CreatedUtc)
    VALUES (@IdempotencyKey, @TriggerId, SYSUTCDATETIME());

    COMMIT TRAN;

    SELECT *, CAST(0 AS BIT) AS AlreadyApplied FROM dbo.InvestmentAccount WHERE Id = @AccountId;
END
GO
