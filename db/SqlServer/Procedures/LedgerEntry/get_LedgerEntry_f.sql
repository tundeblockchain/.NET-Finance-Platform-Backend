/*
  Procedure : dbo.get_LedgerEntry_f
  Purpose   : Fetches a single LedgerEntry row by Id.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_LedgerEntry_f
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.LedgerEntry WHERE Id = @Id;
END
GO
