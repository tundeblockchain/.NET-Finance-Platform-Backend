SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.DistributionAgreement', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DistributionAgreement
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_DistributionAgreement PRIMARY KEY,
        CustomerId      INT              NOT NULL,
        OwnerComponent  INT              NOT NULL,
        OwnerAccountId  UNIQUEIDENTIFIER NOT NULL,
        Name            NVARCHAR(200)    NOT NULL,
        IsActive        BIT              NOT NULL CONSTRAINT DF_DistributionAgreement_IsActive DEFAULT (1),
        CreatedUtc      DATETIMEOFFSET   NOT NULL,
        DateModified    DATETIMEOFFSET   NOT NULL,
        ChangedBy       NVARCHAR(100)    NOT NULL,
        CONSTRAINT FK_DistributionAgreement_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer (Id)
    );

    CREATE INDEX IX_DistributionAgreement_OwnerAccount ON dbo.DistributionAgreement (OwnerAccountId, IsActive);
END
GO
