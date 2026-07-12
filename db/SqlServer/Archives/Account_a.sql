SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.Account_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Account_a
    (
        ArchiveId       BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Account_a PRIMARY KEY,
        ArchivedUtc     DATETIMEOFFSET NOT NULL CONSTRAINT DF_Account_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id              UNIQUEIDENTIFIER NOT NULL,
        CustomerId      UNIQUEIDENTIFIER NOT NULL,
        AccountNumber   NVARCHAR(64)     NOT NULL,
        Currency        NVARCHAR(3)      NOT NULL,
        IsActive        BIT              NOT NULL,
        CreatedUtc      DATETIMEOFFSET   NOT NULL,
        DateModified    DATETIMEOFFSET   NOT NULL,
        ChangedBy       NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_Account_a_Id ON dbo.Account_a (Id, ArchivedUtc);
END
GO
