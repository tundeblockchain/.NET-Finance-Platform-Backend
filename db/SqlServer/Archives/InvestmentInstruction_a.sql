SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.InvestmentInstruction_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvestmentInstruction_a
    (
        ArchiveId       BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_InvestmentInstruction_a PRIMARY KEY,
        ArchivedUtc     DATETIMEOFFSET NOT NULL CONSTRAINT DF_InvestmentInstruction_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id                   UNIQUEIDENTIFIER NOT NULL,
        CustomerId           INT              NOT NULL,
        TradingAccountId     UNIQUEIDENTIFIER NOT NULL,
        InvestmentAccountId  UNIQUEIDENTIFIER NOT NULL,
        AssetSymbol          NVARCHAR(32)     NOT NULL,
        Quantity             DECIMAL(18,8)    NOT NULL,
        CashAmount           DECIMAL(18,4)    NOT NULL,
        Currency             NVARCHAR(3)      NOT NULL,
        Side                 INT              NOT NULL,
        Status               INT              NOT NULL,
        OrderId              UNIQUEIDENTIFIER NULL,
        IdempotencyKey       NVARCHAR(200)    NOT NULL,
        CreatedUtc           DATETIMEOFFSET   NOT NULL,
        DateModified         DATETIMEOFFSET   NOT NULL,
        ChangedBy            NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_InvestmentInstruction_a_Id ON dbo.InvestmentInstruction_a (Id, ArchivedUtc);
END
GO
