/*
  Procedure : dbo.get_InvestmentAccount_ByTradingAccount_f
  Purpose   : Fetches the InvestmentAccount for a TradingAccount.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_InvestmentAccount_ByTradingAccount_f
    @TradingAccountId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.InvestmentAccount WHERE TradingAccountId = @TradingAccountId;
END
GO
