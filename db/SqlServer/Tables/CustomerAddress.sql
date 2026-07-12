SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.CustomerAddress', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerAddress
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CustomerAddress PRIMARY KEY,
        CustomerId      INT              NOT NULL,
        Line1           NVARCHAR(200)    NOT NULL,
        Line2           NVARCHAR(200)    NULL,
        City            NVARCHAR(100)    NOT NULL,
        Region          NVARCHAR(100)    NULL,
        PostalCode      NVARCHAR(32)     NOT NULL,
        Country         NVARCHAR(100)    NOT NULL,
        DateModified    DATETIMEOFFSET   NOT NULL,
        ChangedBy       NVARCHAR(100)    NOT NULL,
        CONSTRAINT FK_CustomerAddress_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer (Id)
    );

    CREATE UNIQUE INDEX UX_CustomerAddress_CustomerId ON dbo.CustomerAddress (CustomerId);
END
GO
