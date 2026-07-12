/*
  Procedure : dbo.get_SystemEventWorking_f
  Purpose   : Fetches a SystemEventWorking lease row by TriggerId. No archive table.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_SystemEventWorking_f
    @TriggerId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.SystemEventWorking WHERE TriggerId = @TriggerId;
END
GO
