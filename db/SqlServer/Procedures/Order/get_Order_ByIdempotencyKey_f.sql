/*
  Procedure : dbo.get_Order_ByIdempotencyKey_f
  Purpose   : Fetches an Order by IdempotencyKey.
  Dated     : 2026-07-14
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_Order_ByIdempotencyKey_f
    @IdempotencyKey NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.[Order] WHERE IdempotencyKey = @IdempotencyKey;
END
GO
