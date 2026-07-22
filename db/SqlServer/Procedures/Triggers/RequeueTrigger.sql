/*
  Procedure : dbo.RequeueTrigger
  Purpose   : Manually requeues a Failed trigger after the underlying issue is resolved.
              Sets Status=Pending, clears CompletedUtc, optionally resets AttemptCount,
              schedules NextAttemptUtc (defaults to now), and removes any SystemEventWorking lease.
              Returns the updated trigger row. Raises an error if the trigger is missing or not Failed.
  Dated     : 2026-07-22
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.RequeueTrigger
    @TriggerId UNIQUEIDENTIFIER,
    @NextAttemptUtc DATETIMEOFFSET = NULL,
    @ResetAttemptCount BIT = 1,
    @ChangedBy NVARCHAR(100) = N'operator'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();
    IF @NextAttemptUtc IS NULL SET @NextAttemptUtc = @Now;
    IF @ChangedBy IS NULL OR LTRIM(RTRIM(@ChangedBy)) = N'' SET @ChangedBy = N'operator';

    BEGIN TRANSACTION;

    DECLARE @Status INT;
    DECLARE @TriggerIdText NVARCHAR(36) = CONVERT(NVARCHAR(36), @TriggerId);

    SELECT @Status = Status
    FROM dbo.SystemEventTrigger WITH (UPDLOCK, ROWLOCK)
    WHERE Id = @TriggerId;

    IF @Status IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR(N'RequeueTrigger: trigger %s was not found.', 16, 1, @TriggerIdText);
        RETURN;
    END;

    /* Failed = 5 */
    IF @Status <> 5
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR(N'RequeueTrigger: trigger %s must be Failed (5); current status=%d.', 16, 1, @TriggerIdText, @Status);
        RETURN;
    END;

    UPDATE dbo.SystemEventTrigger
    SET Status = 0, /* Pending */
        CompletedUtc = NULL,
        NextAttemptUtc = @NextAttemptUtc,
        AttemptCount = CASE WHEN @ResetAttemptCount = 1 THEN 0 ELSE AttemptCount END,
        LastError = N'requeued after failure',
        DateModified = @Now,
        ChangedBy = @ChangedBy
    WHERE Id = @TriggerId;

    DELETE FROM dbo.SystemEventWorking WHERE TriggerId = @TriggerId;

    COMMIT TRANSACTION;

    SELECT *
    FROM dbo.SystemEventTrigger
    WHERE Id = @TriggerId;
END
GO
