SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.InvestmentInstruction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvestmentInstruction
    (
        Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_InvestmentInstruction PRIMARY KEY,
        CustomerId           INT              NOT NULL,
        TradingAccountId     UNIQUEIDENTIFIER NOT NULL,
        InvestmentAccountId  UNIQUEIDENTIFIER NOT NULL,
        AssetSymbol          NVARCHAR(32)     NOT NULL,
        Quantity             DECIMAL(18,8)    NOT NULL,
        CashAmount           DECIMAL(18,4)    NOT NULL,
        Currency             NVARCHAR(3)      NOT NULL,
        Side                 INT              NOT NULL,
        Status               INT              NOT NULL CONSTRAINT DF_InvestmentInstruction_Status DEFAULT (0),
        OrderId              UNIQUEIDENTIFIER NULL,
        IdempotencyKey       NVARCHAR(200)    NOT NULL,
        CreatedUtc           DATETIMEOFFSET   NOT NULL,
        DateModified         DATETIMEOFFSET   NOT NULL,
        ChangedBy            NVARCHAR(100)    NOT NULL,
        CONSTRAINT UQ_InvestmentInstruction_IdempotencyKey UNIQUE (IdempotencyKey),
        CONSTRAINT FK_InvestmentInstruction_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer (Id),
        CONSTRAINT FK_InvestmentInstruction_TradingAccount FOREIGN KEY (TradingAccountId) REFERENCES dbo.TradingAccount (Id),
        CONSTRAINT FK_InvestmentInstruction_InvestmentAccount FOREIGN KEY (InvestmentAccountId) REFERENCES dbo.InvestmentAccount (Id)
    );

    CREATE INDEX IX_InvestmentInstruction_Trading_Status ON dbo.InvestmentInstruction (TradingAccountId, Status);
    CREATE INDEX IX_InvestmentInstruction_InvestmentAccount ON dbo.InvestmentInstruction (InvestmentAccountId);
END
GO
