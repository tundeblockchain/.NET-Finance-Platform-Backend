SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.DistributionElement_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DistributionElement_a
    (
        ArchiveId       BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_DistributionElement_a PRIMARY KEY,
        ArchivedUtc     DATETIMEOFFSET NOT NULL CONSTRAINT DF_DistributionElement_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id              UNIQUEIDENTIFIER NOT NULL,
        AgreementId     UNIQUEIDENTIFIER NOT NULL,
        TargetType      INT              NOT NULL,
        TargetAccountId UNIQUEIDENTIFIER NOT NULL,
        Percentage      DECIMAL(9,6)     NOT NULL,
        Priority        INT              NOT NULL,
        DateModified    DATETIMEOFFSET   NOT NULL,
        ChangedBy       NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_DistributionElement_a_Id ON dbo.DistributionElement_a (Id, ArchivedUtc);
END
GO
