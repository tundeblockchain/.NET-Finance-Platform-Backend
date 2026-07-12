/*
  Procedure : dbo.DistributionAgreement_u
  Purpose   : Upserts a DistributionAgreement. On update, archives into DistributionAgreement_a. Sets DateModified and ChangedBy.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.DistributionAgreement_u
    @Id UNIQUEIDENTIFIER,
    @CustomerId INT,
    @OwnerComponent INT,
    @OwnerAccountId UNIQUEIDENTIFIER,
    @Name NVARCHAR(200),
    @IsActive BIT,
    @CreatedUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.DistributionAgreement WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.DistributionAgreement_a (Id, CustomerId, OwnerComponent, OwnerAccountId, Name, IsActive, CreatedUtc, DateModified, ChangedBy)
        SELECT Id, CustomerId, OwnerComponent, OwnerAccountId, Name, IsActive, CreatedUtc, DateModified, ChangedBy
        FROM dbo.DistributionAgreement WHERE Id = @Id;

        UPDATE dbo.DistributionAgreement
        SET CustomerId = @CustomerId, OwnerComponent = @OwnerComponent, OwnerAccountId = @OwnerAccountId,
            Name = @Name, IsActive = @IsActive, CreatedUtc = @CreatedUtc,
            DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.DistributionAgreement (Id, CustomerId, OwnerComponent, OwnerAccountId, Name, IsActive, CreatedUtc, DateModified, ChangedBy)
        VALUES (@Id, @CustomerId, @OwnerComponent, @OwnerAccountId, @Name, @IsActive, @CreatedUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.DistributionAgreement WHERE Id = @Id;
END
GO
