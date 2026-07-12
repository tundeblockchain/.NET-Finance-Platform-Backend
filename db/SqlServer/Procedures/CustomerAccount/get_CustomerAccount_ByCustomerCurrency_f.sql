/*
  Procedure : dbo.get_CustomerAccount_ByCustomerCurrency_f
  Purpose   : Fetches a CustomerAccount by CustomerId and Currency.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_CustomerAccount_ByCustomerCurrency_f
    @CustomerId INT,
    @Currency NVARCHAR(3)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.CustomerAccount WHERE CustomerId = @CustomerId AND Currency = @Currency;
END
GO
