SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.CustomerAccount', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerAccount
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CustomerAccount PRIMARY KEY,
        CustomerId          INT              NOT NULL,
        Currency            NVARCHAR(3)      NOT NULL,
        Settled             DECIMAL(18,4)    NOT NULL CONSTRAINT DF_CustomerAccount_Settled DEFAULT (0),
        Reserved            DECIMAL(18,4)    NOT NULL CONSTRAINT DF_CustomerAccount_Reserved DEFAULT (0),
        IsLocked            BIT              NOT NULL CONSTRAINT DF_CustomerAccount_IsLocked DEFAULT (0),
        LockedByTriggerId   UNIQUEIDENTIFIER NULL,
        LockExpiresUtc      DATETIMEOFFSET   NULL,
        CreatedUtc          DATETIMEOFFSET   NOT NULL,
        DateModified        DATETIMEOFFSET   NOT NULL,
        ChangedBy           NVARCHAR(100)    NOT NULL,
        CONSTRAINT FK_CustomerAccount_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer (Id)
    );

    CREATE UNIQUE INDEX UX_CustomerAccount_Customer_Currency ON dbo.CustomerAccount (CustomerId, Currency);
END
GO
