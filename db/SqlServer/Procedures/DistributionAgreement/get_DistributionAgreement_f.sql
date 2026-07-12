/*
  Procedure : dbo.get_DistributionAgreement_f
  Purpose   : Fetches a DistributionAgreement by Id.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_DistributionAgreement_f
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.DistributionAgreement WHERE Id = @Id;
END
GO
