/*
  Procedure : dbo.get_CashBalance_f
  Purpose   : Fetches a single CashBalance row by Id.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_CashBalance_f
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.CashBalance WHERE Id = @Id;
END
GO
