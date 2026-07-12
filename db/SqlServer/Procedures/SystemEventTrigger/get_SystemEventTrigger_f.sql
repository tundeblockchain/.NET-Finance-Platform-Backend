/*
  Procedure : dbo.get_SystemEventTrigger_f
  Purpose   : Fetches a SystemEventTrigger row by Id. No archive table.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_SystemEventTrigger_f
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.SystemEventTrigger WHERE Id = @Id;
END
GO
