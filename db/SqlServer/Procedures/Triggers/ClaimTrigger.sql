/*
  Procedure : dbo.ClaimTrigger
  Purpose   : Claims the next pending trigger for a queue using UPDLOCK/READPAST. Moves it to Running, inserts SystemEventWorking, and returns trigger + working result sets. Empty sets when no work is available.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.ClaimTrigger
    @QueueName NVARCHAR(64),
    @WorkerInstanceId NVARCHAR(200),
    @LeaseSeconds INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TriggerId UNIQUEIDENTIFIER;
    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    BEGIN TRANSACTION;

    SELECT TOP (1) @TriggerId = Id
    FROM dbo.SystemEventTrigger WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE QueueName = @QueueName
      AND Status = 0
      AND (NextAttemptUtc IS NULL OR NextAttemptUtc <= @Now)
      AND NOT EXISTS (SELECT 1 FROM dbo.SystemEventWorking WHERE TriggerId = dbo.SystemEventTrigger.Id)
    ORDER BY CreatedUtc;

    IF @TriggerId IS NULL
    BEGIN
        COMMIT TRANSACTION;
        SELECT TOP (0) * FROM dbo.SystemEventTrigger;
        SELECT TOP (0) * FROM dbo.SystemEventWorking;
        RETURN;
    END

    UPDATE dbo.SystemEventTrigger
    SET Status = 2, AttemptCount = AttemptCount + 1, DateModified = @Now, ChangedBy = N'broker'
    WHERE Id = @TriggerId;

    INSERT INTO dbo.SystemEventWorking
        (TriggerId, WorkerInstanceId, QueueName, ClaimedUtc, HeartbeatUtc, LeaseExpiresUtc, DateModified, ChangedBy)
    VALUES
        (@TriggerId, @WorkerInstanceId, @QueueName, @Now, @Now, DATEADD(SECOND, @LeaseSeconds, @Now), @Now, N'broker');

    COMMIT TRANSACTION;

    SELECT * FROM dbo.SystemEventTrigger WHERE Id = @TriggerId;
    SELECT * FROM dbo.SystemEventWorking WHERE TriggerId = @TriggerId;
END
GO
