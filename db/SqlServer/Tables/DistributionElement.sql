SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.DistributionElement', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DistributionElement
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_DistributionElement PRIMARY KEY,
        AgreementId     UNIQUEIDENTIFIER NOT NULL,
        TargetType      INT              NOT NULL,
        TargetAccountId UNIQUEIDENTIFIER NOT NULL,
        Percentage      DECIMAL(9,6)     NOT NULL,
        Priority        INT              NOT NULL CONSTRAINT DF_DistributionElement_Priority DEFAULT (1),
        DateModified    DATETIMEOFFSET   NOT NULL,
        ChangedBy       NVARCHAR(100)    NOT NULL,
        CONSTRAINT FK_DistributionElement_Agreement FOREIGN KEY (AgreementId) REFERENCES dbo.DistributionAgreement (Id)
    );

    CREATE INDEX IX_DistributionElement_Agreement ON dbo.DistributionElement (AgreementId, Priority);
END
GO
