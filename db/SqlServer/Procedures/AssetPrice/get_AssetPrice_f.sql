/*
  Procedure : dbo.get_AssetPrice_f
  Purpose   : Fetches a single AssetPrice row by Id.
  Dated     : 2026-07-14
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_AssetPrice_f
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.AssetPrice WHERE Id = @Id;
END
GO
