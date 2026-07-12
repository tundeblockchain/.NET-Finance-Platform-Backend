/*
  Procedure : dbo.get_DistributionElement_f
  Purpose   : Fetches a DistributionElement by Id.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_DistributionElement_f
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.DistributionElement WHERE Id = @Id;
END
GO
