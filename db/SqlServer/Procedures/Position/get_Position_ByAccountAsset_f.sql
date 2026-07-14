/*
  Procedure : dbo.get_Position_ByAccountAsset_f
  Purpose   : Fetches a Position by AccountId and AssetSymbol.
  Dated     : 2026-07-14
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_Position_ByAccountAsset_f
    @AccountId UNIQUEIDENTIFIER,
    @AssetSymbol NVARCHAR(32)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Position WHERE AccountId = @AccountId AND AssetSymbol = @AssetSymbol;
END
GO
