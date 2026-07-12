SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.RetryTrigger
    @TriggerId UNIQUEIDENTIFIER,
    @Error NVARCHAR(2000),
    @NextAttemptUtc DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    BEGIN TRANSACTION;

    UPDATE dbo.SystemEventTrigger
    SET Status = 0, LastError = @Error, NextAttemptUtc = @NextAttemptUtc,
        DateModified = @Now, ChangedBy = N'broker'
    WHERE Id = @TriggerId;

    DELETE FROM dbo.SystemEventWorking WHERE TriggerId = @TriggerId;

    COMMIT TRANSACTION;
END
GO
