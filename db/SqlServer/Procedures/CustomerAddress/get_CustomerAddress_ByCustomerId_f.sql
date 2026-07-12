/*
  Procedure : dbo.get_CustomerAddress_ByCustomerId_f
  Purpose   : Fetches a CustomerAddress by CustomerId.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_CustomerAddress_ByCustomerId_f
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.CustomerAddress WHERE CustomerId = @CustomerId;
END
GO
