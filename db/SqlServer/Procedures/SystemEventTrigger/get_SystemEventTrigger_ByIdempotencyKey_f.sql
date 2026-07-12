/*
  Procedure : dbo.get_SystemEventTrigger_ByIdempotencyKey_f
  Purpose   : Fetches a SystemEventTrigger row by IdempotencyKey. No archive table.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_SystemEventTrigger_ByIdempotencyKey_f
    @IdempotencyKey NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.SystemEventTrigger WHERE IdempotencyKey = @IdempotencyKey;
END
GO
