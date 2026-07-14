/*
  Procedure : dbo.get_LedgerEntry_Count_f
  Purpose   : Returns total ledger entry count.
  Dated     : 2026-07-14
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_LedgerEntry_Count_f
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(1) AS EntryCount FROM dbo.LedgerEntry;
END
GO
