SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.TradingAccount', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TradingAccount
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TradingAccount PRIMARY KEY,
        CustomerId          INT              NOT NULL,
        Currency            NVARCHAR(3)      NOT NULL,
        Settled             DECIMAL(18,4)    NOT NULL CONSTRAINT DF_TradingAccount_Settled DEFAULT (0),
        Reserved            DECIMAL(18,4)    NOT NULL CONSTRAINT DF_TradingAccount_Reserved DEFAULT (0),
        IsLocked            BIT              NOT NULL CONSTRAINT DF_TradingAccount_IsLocked DEFAULT (0),
        LockedByTriggerId   UNIQUEIDENTIFIER NULL,
        LockExpiresUtc      DATETIMEOFFSET   NULL,
        CreatedUtc          DATETIMEOFFSET   NOT NULL,
        DateModified        DATETIMEOFFSET   NOT NULL,
        ChangedBy           NVARCHAR(100)    NOT NULL,
        CONSTRAINT FK_TradingAccount_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer (Id)
    );

    CREATE UNIQUE INDEX UX_TradingAccount_Customer_Currency ON dbo.TradingAccount (CustomerId, Currency);
END
GO
