/*
  Procedure : dbo.get_Order_ByAccountId_f
  Purpose   : Lists orders for an account, newest first.
  Dated     : 2026-07-14
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_Order_ByAccountId_f
    @AccountId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM dbo.[Order]
    WHERE AccountId = @AccountId
    ORDER BY CreatedUtc DESC;
END
GO
