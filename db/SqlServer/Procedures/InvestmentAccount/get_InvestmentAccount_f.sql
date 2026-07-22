/*
  Procedure : dbo.get_InvestmentAccount_f
  Purpose   : Fetches an InvestmentAccount by Id.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_InvestmentAccount_f
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.InvestmentAccount WHERE Id = @Id;
END
GO
