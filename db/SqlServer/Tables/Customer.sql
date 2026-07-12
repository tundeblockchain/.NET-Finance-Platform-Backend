SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.Customer', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customer
    (
        Id              INT              NOT NULL IDENTITY(1,1) CONSTRAINT PK_Customer PRIMARY KEY,
        Email           NVARCHAR(256)    NOT NULL,
        FirstName       NVARCHAR(100)    NOT NULL,
        LastName        NVARCHAR(100)    NOT NULL,
        CreatedUtc      DATETIMEOFFSET   NOT NULL,
        DateModified    DATETIMEOFFSET   NOT NULL,
        ChangedBy       NVARCHAR(100)    NOT NULL
    );

    CREATE UNIQUE INDEX UX_Customer_Email ON dbo.Customer (Email);
END
GO
