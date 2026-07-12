SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.CustomerAddress_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerAddress_a
    (
        ArchiveId       BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_CustomerAddress_a PRIMARY KEY,
        ArchivedUtc     DATETIMEOFFSET NOT NULL CONSTRAINT DF_CustomerAddress_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id              UNIQUEIDENTIFIER NOT NULL,
        CustomerId      INT              NOT NULL,
        Line1           NVARCHAR(200)    NOT NULL,
        Line2           NVARCHAR(200)    NULL,
        City            NVARCHAR(100)    NOT NULL,
        Region          NVARCHAR(100)    NULL,
        PostalCode      NVARCHAR(32)     NOT NULL,
        Country         NVARCHAR(100)    NOT NULL,
        DateModified    DATETIMEOFFSET   NOT NULL,
        ChangedBy       NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_CustomerAddress_a_Id ON dbo.CustomerAddress_a (Id, ArchivedUtc);
END
GO
