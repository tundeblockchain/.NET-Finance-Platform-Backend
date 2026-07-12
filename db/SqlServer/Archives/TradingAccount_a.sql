SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.TradingAccount_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TradingAccount_a
    (
        ArchiveId       BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_TradingAccount_a PRIMARY KEY,
        ArchivedUtc     DATETIMEOFFSET NOT NULL CONSTRAINT DF_TradingAccount_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id                  UNIQUEIDENTIFIER NOT NULL,
        CustomerId          INT              NOT NULL,
        Currency            NVARCHAR(3)      NOT NULL,
        Settled             DECIMAL(18,4)    NOT NULL,
        Reserved            DECIMAL(18,4)    NOT NULL,
        IsLocked            BIT              NOT NULL,
        LockedByTriggerId   UNIQUEIDENTIFIER NULL,
        LockExpiresUtc      DATETIMEOFFSET   NULL,
        CreatedUtc          DATETIMEOFFSET   NOT NULL,
        DateModified        DATETIMEOFFSET   NOT NULL,
        ChangedBy           NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_TradingAccount_a_Id ON dbo.TradingAccount_a (Id, ArchivedUtc);
END
GO
