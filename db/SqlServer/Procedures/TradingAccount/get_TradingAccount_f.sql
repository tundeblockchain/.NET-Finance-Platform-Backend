/*
  Procedure : dbo.get_TradingAccount_f
  Purpose   : Fetches a TradingAccount by Id.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_TradingAccount_f
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.TradingAccount WHERE Id = @Id;
END
GO
