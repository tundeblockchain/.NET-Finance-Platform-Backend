SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.SystemEventWorking_u
    @TriggerId UNIQUEIDENTIFIER,
    @WorkerInstanceId NVARCHAR(200),
    @QueueName NVARCHAR(64),
    @ClaimedUtc DATETIMEOFFSET,
    @HeartbeatUtc DATETIMEOFFSET,
    @LeaseExpiresUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.SystemEventWorking WHERE TriggerId = @TriggerId)
    BEGIN
        UPDATE dbo.SystemEventWorking
        SET WorkerInstanceId = @WorkerInstanceId, QueueName = @QueueName, ClaimedUtc = @ClaimedUtc,
            HeartbeatUtc = @HeartbeatUtc, LeaseExpiresUtc = @LeaseExpiresUtc,
            DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE TriggerId = @TriggerId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.SystemEventWorking
            (TriggerId, WorkerInstanceId, QueueName, ClaimedUtc, HeartbeatUtc, LeaseExpiresUtc, DateModified, ChangedBy)
        VALUES
            (@TriggerId, @WorkerInstanceId, @QueueName, @ClaimedUtc, @HeartbeatUtc, @LeaseExpiresUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.SystemEventWorking WHERE TriggerId = @TriggerId;
END
GO
