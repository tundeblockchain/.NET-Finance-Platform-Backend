/*
  Procedure : dbo.get_CashBalance_ByAccountCurrency_f
  Purpose   : Fetches a CashBalance row by AccountId and Currency.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.get_CashBalance_ByAccountCurrency_f
    @AccountId UNIQUEIDENTIFIER,
    @Currency NVARCHAR(3)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.CashBalance WHERE AccountId = @AccountId AND Currency = @Currency;
END
GO
