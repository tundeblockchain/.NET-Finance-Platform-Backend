/*
  Procedure : dbo.get_InvestmentInstruction_PendingCashByTradingAccount_f
  Purpose   : Sums CashAmount for Pending/Processing instructions on a trading account.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_InvestmentInstruction_PendingCashByTradingAccount_f
    @TradingAccountId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    -- Status: Pending = 0, Processing = 1
    SELECT CAST(ISNULL(SUM(CashAmount), 0) AS DECIMAL(18,4)) AS PendingCashAmount
    FROM dbo.InvestmentInstruction
    WHERE TradingAccountId = @TradingAccountId
      AND Status IN (0, 1);
END
GO
