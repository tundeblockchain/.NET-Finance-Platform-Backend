SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.HeartbeatTrigger
    @TriggerId UNIQUEIDENTIFIER,
    @WorkerInstanceId NVARCHAR(200),
    @LeaseSeconds INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    UPDATE dbo.SystemEventWorking
    SET HeartbeatUtc = @Now, LeaseExpiresUtc = DATEADD(SECOND, @LeaseSeconds, @Now),
        DateModified = @Now, ChangedBy = N'broker'
    WHERE TriggerId = @TriggerId
      AND WorkerInstanceId = @WorkerInstanceId;

    IF @@ROWCOUNT = 0
    BEGIN
        THROW 50001, 'The trigger is not owned by this worker.', 1;
    END
END
GO
