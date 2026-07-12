/*
  Procedure : dbo.DistributionElement_u
  Purpose   : Upserts a DistributionElement. On update, archives into DistributionElement_a. Sets DateModified and ChangedBy.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.DistributionElement_u
    @Id UNIQUEIDENTIFIER,
    @AgreementId UNIQUEIDENTIFIER,
    @TargetType INT,
    @TargetAccountId UNIQUEIDENTIFIER,
    @Percentage DECIMAL(9,6),
    @Priority INT,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.DistributionElement WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.DistributionElement_a (Id, AgreementId, TargetType, TargetAccountId, Percentage, Priority, DateModified, ChangedBy)
        SELECT Id, AgreementId, TargetType, TargetAccountId, Percentage, Priority, DateModified, ChangedBy
        FROM dbo.DistributionElement WHERE Id = @Id;

        UPDATE dbo.DistributionElement
        SET AgreementId = @AgreementId, TargetType = @TargetType, TargetAccountId = @TargetAccountId,
            Percentage = @Percentage, Priority = @Priority, DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.DistributionElement (Id, AgreementId, TargetType, TargetAccountId, Percentage, Priority, DateModified, ChangedBy)
        VALUES (@Id, @AgreementId, @TargetType, @TargetAccountId, @Percentage, @Priority, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.DistributionElement WHERE Id = @Id;
END
GO
