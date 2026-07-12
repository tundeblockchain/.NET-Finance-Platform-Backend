/*
  Procedure : dbo.get_DistributionAgreement_ByOwnerAccount_f
  Purpose   : Fetches the active DistributionAgreement for an owner account.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_DistributionAgreement_ByOwnerAccount_f
    @OwnerAccountId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) * FROM dbo.DistributionAgreement
    WHERE OwnerAccountId = @OwnerAccountId AND IsActive = 1
    ORDER BY CreatedUtc DESC;
END
GO
