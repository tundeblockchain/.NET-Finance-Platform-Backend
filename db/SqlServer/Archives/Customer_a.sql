SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.Customer_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customer_a
    (
        ArchiveId       BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Customer_a PRIMARY KEY,
        ArchivedUtc     DATETIMEOFFSET NOT NULL CONSTRAINT DF_Customer_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id              INT              NOT NULL,
        Email           NVARCHAR(256)    NOT NULL,
        FirstName       NVARCHAR(100)    NOT NULL,
        LastName        NVARCHAR(100)    NOT NULL,
        CreatedUtc      DATETIMEOFFSET   NOT NULL,
        DateModified    DATETIMEOFFSET   NOT NULL,
        ChangedBy       NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_Customer_a_Id ON dbo.Customer_a (Id, ArchivedUtc);
END
GO
