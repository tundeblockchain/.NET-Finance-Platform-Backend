/*
  Procedure : dbo.MarkCompensationTrigger
  Purpose   : Marks a trigger as Compensation and removes its SystemEventWorking lease. Sets ChangedBy to broker.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.MarkCompensationTrigger
    @TriggerId UNIQUEIDENTIFIER,
    @Error NVARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    BEGIN TRANSACTION;

    UPDATE dbo.SystemEventTrigger
    SET Status = 6, LastError = @Error, DateModified = @Now, ChangedBy = N'broker'
    WHERE Id = @TriggerId;

    DELETE FROM dbo.SystemEventWorking WHERE TriggerId = @TriggerId;

    COMMIT TRANSACTION;
END
GO
