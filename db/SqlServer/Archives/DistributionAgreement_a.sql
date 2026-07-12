SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.DistributionAgreement_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DistributionAgreement_a
    (
        ArchiveId       BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_DistributionAgreement_a PRIMARY KEY,
        ArchivedUtc     DATETIMEOFFSET NOT NULL CONSTRAINT DF_DistributionAgreement_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id              UNIQUEIDENTIFIER NOT NULL,
        CustomerId      INT              NOT NULL,
        OwnerComponent  INT              NOT NULL,
        OwnerAccountId  UNIQUEIDENTIFIER NOT NULL,
        Name            NVARCHAR(200)    NOT NULL,
        IsActive        BIT              NOT NULL,
        CreatedUtc      DATETIMEOFFSET   NOT NULL,
        DateModified    DATETIMEOFFSET   NOT NULL,
        ChangedBy       NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_DistributionAgreement_a_Id ON dbo.DistributionAgreement_a (Id, ArchivedUtc);
END
GO
