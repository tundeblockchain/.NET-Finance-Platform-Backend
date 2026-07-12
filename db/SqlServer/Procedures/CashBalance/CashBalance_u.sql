/*
  Procedure : dbo.CashBalance_u
  Purpose   : Upserts a CashBalance. On update, archives the current row into CashBalance_a before applying changes. Sets DateModified and ChangedBy.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.CashBalance_u
    @Id UNIQUEIDENTIFIER,
    @AccountId UNIQUEIDENTIFIER,
    @Currency NVARCHAR(3),
    @Settled DECIMAL(18, 4),
    @Reserved DECIMAL(18, 4),
    @IsLocked BIT,
    @LockedByAllocationId UNIQUEIDENTIFIER,
    @LockedByTriggerId UNIQUEIDENTIFIER,
    @LockAcquiredUtc DATETIMEOFFSET,
    @LockExpiresUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.CashBalance WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.CashBalance_a (Id, AccountId, Currency, Settled, Reserved, IsLocked, LockedByAllocationId, LockedByTriggerId, LockAcquiredUtc, LockExpiresUtc, DateModified, ChangedBy)
        SELECT Id, AccountId, Currency, Settled, Reserved, IsLocked, LockedByAllocationId, LockedByTriggerId, LockAcquiredUtc, LockExpiresUtc, DateModified, ChangedBy
        FROM dbo.CashBalance WHERE Id = @Id;

        UPDATE dbo.CashBalance
        SET AccountId = @AccountId, Currency = @Currency, Settled = @Settled, Reserved = @Reserved, IsLocked = @IsLocked,
            LockedByAllocationId = @LockedByAllocationId, LockedByTriggerId = @LockedByTriggerId,
            LockAcquiredUtc = @LockAcquiredUtc, LockExpiresUtc = @LockExpiresUtc,
            DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.CashBalance (Id, AccountId, Currency, Settled, Reserved, IsLocked, LockedByAllocationId, LockedByTriggerId, LockAcquiredUtc, LockExpiresUtc, DateModified, ChangedBy)
        VALUES (@Id, @AccountId, @Currency, @Settled, @Reserved, @IsLocked, @LockedByAllocationId, @LockedByTriggerId, @LockAcquiredUtc, @LockExpiresUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.CashBalance WHERE Id = @Id;
END
GO
