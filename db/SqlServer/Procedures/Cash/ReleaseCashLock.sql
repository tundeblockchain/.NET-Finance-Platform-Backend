/*
  Procedure : dbo.ReleaseCashLock
  Purpose   : Releases a cash lock only when LockedByTriggerId matches the caller. Returns rows affected.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.ReleaseCashLock
    @AccountId UNIQUEIDENTIFIER,
    @Currency NVARCHAR(3),
    @TriggerId UNIQUEIDENTIFIER,
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    UPDATE dbo.CashBalance
    SET IsLocked = 0,
        LockedByAllocationId = NULL,
        LockedByTriggerId = NULL,
        LockAcquiredUtc = NULL,
        LockExpiresUtc = NULL,
        DateModified = @Now,
        ChangedBy = @ChangedBy
    WHERE AccountId = @AccountId
      AND Currency = @Currency
      AND IsLocked = 1
      AND LockedByTriggerId = @TriggerId;

    SELECT CAST(@@ROWCOUNT AS INT) AS RowsAffected;
END
GO
