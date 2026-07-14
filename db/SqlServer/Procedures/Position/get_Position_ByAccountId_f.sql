/*
  Procedure : dbo.get_Position_ByAccountId_f
  Purpose   : Lists positions for an account with non-zero quantity.
  Dated     : 2026-07-14
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_Position_ByAccountId_f
    @AccountId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM dbo.Position
    WHERE AccountId = @AccountId AND Quantity <> 0
    ORDER BY AssetSymbol;
END
GO
