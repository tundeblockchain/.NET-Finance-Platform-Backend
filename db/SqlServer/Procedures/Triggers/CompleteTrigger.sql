/*
  Procedure : dbo.CompleteTrigger
  Purpose   : Marks a trigger Completed, stores ResultJson, and removes its SystemEventWorking lease. Sets ChangedBy to broker.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.CompleteTrigger
    @TriggerId UNIQUEIDENTIFIER,
    @ResultJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    BEGIN TRANSACTION;

    UPDATE dbo.SystemEventTrigger
    SET Status = 3, ResultJson = @ResultJson, CompletedUtc = @Now, LastError = NULL,
        DateModified = @Now, ChangedBy = N'broker'
    WHERE Id = @TriggerId;

    DELETE FROM dbo.SystemEventWorking WHERE TriggerId = @TriggerId;

    COMMIT TRANSACTION;
END
GO
