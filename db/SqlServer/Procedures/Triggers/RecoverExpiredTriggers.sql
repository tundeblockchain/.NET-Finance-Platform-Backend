/*
  Procedure : dbo.RecoverExpiredTriggers
  Purpose   : Recovers orphaned working leases (LeaseExpiresUtc <= now). Requeues triggers as Pending with LastError = lease expired, deletes SystemEventWorking. Returns recovered trigger rows. Safe under concurrency via UPDLOCK/ROWLOCK.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.RecoverExpiredTriggers
    @BatchSize INT = 50,
    @NextAttemptUtc DATETIMEOFFSET = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @BatchSize < 1 SET @BatchSize = 1;
    IF @BatchSize > 500 SET @BatchSize = 500;

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();
    IF @NextAttemptUtc IS NULL SET @NextAttemptUtc = @Now;

    DECLARE @Recovered TABLE (TriggerId UNIQUEIDENTIFIER PRIMARY KEY);

    BEGIN TRAN;

    ;WITH Expired AS (
        SELECT TOP (@BatchSize) w.TriggerId
        FROM dbo.SystemEventWorking w WITH (UPDLOCK, READPAST, ROWLOCK)
        INNER JOIN dbo.SystemEventTrigger t WITH (UPDLOCK, ROWLOCK) ON t.Id = w.TriggerId
        WHERE w.LeaseExpiresUtc <= @Now
          AND t.Status IN (1, 2) /* Claimed, Running */
        ORDER BY w.LeaseExpiresUtc
    )
    INSERT INTO @Recovered (TriggerId)
    SELECT TriggerId FROM Expired;

    UPDATE t
    SET Status = 0, /* Pending */
        LastError = N'lease expired',
        NextAttemptUtc = @NextAttemptUtc,
        DateModified = @Now,
        ChangedBy = N'broker'
    FROM dbo.SystemEventTrigger t
    INNER JOIN @Recovered r ON r.TriggerId = t.Id;

    DELETE w
    FROM dbo.SystemEventWorking w
    INNER JOIN @Recovered r ON r.TriggerId = w.TriggerId;

    COMMIT TRAN;

    SELECT t.*
    FROM dbo.SystemEventTrigger t
    INNER JOIN @Recovered r ON r.TriggerId = t.Id
    ORDER BY t.CreatedUtc;
END
GO
