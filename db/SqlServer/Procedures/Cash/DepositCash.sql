/*
  Procedure : dbo.DepositCash
  Purpose   : Credits Settled cash while the caller holds the cash lock. Idempotent via LedgerEntry.IdempotencyKey (also posts a Credit ledger entry). Requires an active lock owned by TriggerId.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.DepositCash
    @IdempotencyKey NVARCHAR(200),
    @AccountId UNIQUEIDENTIFIER,
    @Currency NVARCHAR(3),
    @Amount DECIMAL(18, 4),
    @TriggerId UNIQUEIDENTIFIER,
    @AllocationRequestId UNIQUEIDENTIFIER = NULL,
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Amount <= 0
    BEGIN
        THROW 50010, 'Deposit amount must be positive.', 1;
    END

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.LedgerEntry WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT b.*, CAST(1 AS BIT) AS AlreadyApplied
        FROM dbo.CashBalance b
        WHERE b.AccountId = @AccountId AND b.Currency = @Currency;
        COMMIT TRAN;
        RETURN;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.CashBalance
        WHERE AccountId = @AccountId
          AND Currency = @Currency
          AND IsLocked = 1
          AND LockedByTriggerId = @TriggerId
          AND (LockExpiresUtc IS NULL OR LockExpiresUtc > @Now)
    )
    BEGIN
        ROLLBACK TRAN;
        THROW 50011, 'Deposit requires an active cash lock owned by the trigger.', 1;
    END

    UPDATE dbo.CashBalance
    SET Settled = Settled + @Amount,
        DateModified = @Now,
        ChangedBy = @ChangedBy
    WHERE AccountId = @AccountId AND Currency = @Currency;

    INSERT INTO dbo.LedgerEntry
    (
        Id, AccountId, TriggerId, AllocationRequestId, EntryType, Amount, Currency,
        IdempotencyKey, Description, PostedUtc, DateModified, ChangedBy
    )
    VALUES
    (
        NEWID(), @AccountId, @TriggerId, @AllocationRequestId, 2 /* Credit */, @Amount, @Currency,
        @IdempotencyKey, N'Cash deposit', @Now, @Now, @ChangedBy
    );

    COMMIT TRAN;

    SELECT b.*, CAST(0 AS BIT) AS AlreadyApplied
    FROM dbo.CashBalance b
    WHERE b.AccountId = @AccountId AND b.Currency = @Currency;
END
GO
