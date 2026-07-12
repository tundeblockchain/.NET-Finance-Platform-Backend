/*
  Procedure : dbo.CreditCustomerAccount
  Purpose   : Credits Settled on a CustomerAccount. Idempotent via BrokerMutation.IdempotencyKey.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.CreditCustomerAccount
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
        THROW 50020, 'Credit amount must be positive.', 1;
    END

    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.BrokerMutation WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT *, CAST(1 AS BIT) AS AlreadyApplied FROM dbo.CustomerAccount WHERE Id = @AccountId;
        COMMIT TRAN;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.CustomerAccount WHERE Id = @AccountId)
    BEGIN
        ROLLBACK TRAN;
        THROW 50021, 'Customer account was not found.', 1;
    END

    UPDATE dbo.CustomerAccount
    SET Settled = Settled + @Amount,
        DateModified = SYSUTCDATETIME(),
        ChangedBy = @ChangedBy
    WHERE Id = @AccountId;

    INSERT INTO dbo.BrokerMutation (IdempotencyKey, TriggerId, CreatedUtc)
    VALUES (@IdempotencyKey, @TriggerId, SYSUTCDATETIME());

    COMMIT TRAN;

    SELECT *, CAST(0 AS BIT) AS AlreadyApplied FROM dbo.CustomerAccount WHERE Id = @AccountId;
END
GO
