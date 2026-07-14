/*
  Procedure : dbo.get_InvestmentInstruction_f
  Purpose   : Fetches an InvestmentInstruction by Id.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_InvestmentInstruction_f
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.InvestmentInstruction WHERE Id = @Id;
END
GO
