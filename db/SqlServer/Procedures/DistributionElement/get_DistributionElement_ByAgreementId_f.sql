/*
  Procedure : dbo.get_DistributionElement_ByAgreementId_f
  Purpose   : Fetches DistributionElements for an agreement ordered by Priority.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_DistributionElement_ByAgreementId_f
    @AgreementId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.DistributionElement WHERE AgreementId = @AgreementId ORDER BY Priority;
END
GO
