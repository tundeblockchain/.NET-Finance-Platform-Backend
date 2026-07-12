/*
  Procedure : dbo.AcquireCashLock
  Purpose   : Atomically acquires a cash lock for AccountId/Currency. Creates the CashBalance row if missing. Does not wait when contended; returns an empty result set. Allows re-acquire by the same TriggerId or after lease expiry.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.AcquireCashLock
    @AccountId UNIQUEIDENTIFIER,
    @Currency NVARCHAR(3),
    @TriggerId UNIQUEIDENTIFIER,
    @AllocationRequestId UNIQUEIDENTIFIER = NULL,
    @LeaseSeconds INT,
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();
    DECLARE @BalanceId UNIQUEIDENTIFIER;

    BEGIN TRAN;

    IF NOT EXISTS (SELECT 1 FROM dbo.CashBalance WHERE AccountId = @AccountId AND Currency = @Currency)
    BEGIN
        INSERT INTO dbo.CashBalance
        (
            Id, AccountId, Currency, Settled, Reserved, IsLocked,
            LockedByAllocationId, LockedByTriggerId, LockAcquiredUtc, LockExpiresUtc,
            DateModified, ChangedBy
        )
        VALUES
        (
            NEWID(), @AccountId, @Currency, 0, 0, 0,
            NULL, NULL, NULL, NULL,
            @Now, @ChangedBy
        );
    END

    UPDATE dbo.CashBalance
    SET IsLocked = 1,
        LockedByAllocationId = @AllocationRequestId,
        LockedByTriggerId = @TriggerId,
        LockAcquiredUtc = ISNULL(LockAcquiredUtc, @Now),
        LockExpiresUtc = DATEADD(SECOND, @LeaseSeconds, @Now),
        DateModified = @Now,
        ChangedBy = @ChangedBy
    WHERE AccountId = @AccountId
      AND Currency = @Currency
      AND (
            IsLocked = 0
            OR LockExpiresUtc IS NULL
            OR LockExpiresUtc <= @Now
            OR LockedByTriggerId = @TriggerId
          );

    IF @@ROWCOUNT = 0
    BEGIN
        COMMIT TRAN;
        SELECT TOP (0) * FROM dbo.CashBalance;
        RETURN;
    END

    COMMIT TRAN;
    SELECT * FROM dbo.CashBalance WHERE AccountId = @AccountId AND Currency = @Currency;
END
GO
