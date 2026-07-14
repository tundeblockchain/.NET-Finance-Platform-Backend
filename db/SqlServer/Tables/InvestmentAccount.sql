SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.InvestmentAccount', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvestmentAccount
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_InvestmentAccount PRIMARY KEY,
        CustomerId          INT              NOT NULL,
        TradingAccountId    UNIQUEIDENTIFIER NOT NULL,
        Currency            NVARCHAR(3)      NOT NULL,
        Settled             DECIMAL(18,4)    NOT NULL CONSTRAINT DF_InvestmentAccount_Settled DEFAULT (0),
        Reserved            DECIMAL(18,4)    NOT NULL CONSTRAINT DF_InvestmentAccount_Reserved DEFAULT (0),
        IsLocked            BIT              NOT NULL CONSTRAINT DF_InvestmentAccount_IsLocked DEFAULT (0),
        LockedByTriggerId   UNIQUEIDENTIFIER NULL,
        LockExpiresUtc      DATETIMEOFFSET   NULL,
        CreatedUtc          DATETIMEOFFSET   NOT NULL,
        DateModified        DATETIMEOFFSET   NOT NULL,
        ChangedBy           NVARCHAR(100)    NOT NULL,
        CONSTRAINT FK_InvestmentAccount_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer (Id),
        CONSTRAINT FK_InvestmentAccount_TradingAccount FOREIGN KEY (TradingAccountId) REFERENCES dbo.TradingAccount (Id)
    );

    CREATE UNIQUE INDEX UX_InvestmentAccount_TradingAccount ON dbo.InvestmentAccount (TradingAccountId);
    CREATE INDEX IX_InvestmentAccount_Customer_Currency ON dbo.InvestmentAccount (CustomerId, Currency);
END
GO
