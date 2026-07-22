/*
  Procedure : dbo.get_LedgerEntry_ByIdempotencyKey_f
  Purpose   : Fetches a LedgerEntry by IdempotencyKey.
  Dated     : 2026-07-14
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_LedgerEntry_ByIdempotencyKey_f
    @IdempotencyKey NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.LedgerEntry WHERE IdempotencyKey = @IdempotencyKey;
END
GO
