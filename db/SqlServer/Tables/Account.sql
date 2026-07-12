SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.Account', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Account
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Account PRIMARY KEY,
        CustomerId      UNIQUEIDENTIFIER NOT NULL,
        AccountNumber   NVARCHAR(64)     NOT NULL,
        Currency        NVARCHAR(3)      NOT NULL,
        IsActive        BIT              NOT NULL CONSTRAINT DF_Account_IsActive DEFAULT (1),
        CreatedUtc      DATETIMEOFFSET   NOT NULL,
        DateModified    DATETIMEOFFSET   NOT NULL,
        ChangedBy       NVARCHAR(100)    NOT NULL
    );
END
GO
