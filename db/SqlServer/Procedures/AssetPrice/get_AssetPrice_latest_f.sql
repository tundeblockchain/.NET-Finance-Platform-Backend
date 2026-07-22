/*
  Procedure : dbo.get_AssetPrice_latest_f
  Purpose   : Returns the latest AssetPrice observation for a symbol (by ObservedUtc).
  Dated     : 2026-07-14
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_AssetPrice_latest_f
    @AssetSymbol NVARCHAR(32)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) *
    FROM dbo.AssetPrice
    WHERE AssetSymbol = @AssetSymbol
    ORDER BY ObservedUtc DESC, DateModified DESC;
END
GO
