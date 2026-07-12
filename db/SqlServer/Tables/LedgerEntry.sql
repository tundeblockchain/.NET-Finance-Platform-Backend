SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.LedgerEntry', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LedgerEntry
    (
        Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LedgerEntry PRIMARY KEY,
        AccountId            UNIQUEIDENTIFIER NOT NULL,
        TriggerId            UNIQUEIDENTIFIER NULL,
        AllocationRequestId  UNIQUEIDENTIFIER NULL,
        EntryType            INT              NOT NULL,
        Amount               DECIMAL(18, 4)   NOT NULL,
        Currency             NVARCHAR(3)      NOT NULL,
        IdempotencyKey       NVARCHAR(200)    NOT NULL,
        Description          NVARCHAR(500)    NOT NULL,
        PostedUtc            DATETIMEOFFSET   NOT NULL,
        DateModified         DATETIMEOFFSET   NOT NULL,
        ChangedBy            NVARCHAR(100)    NOT NULL,
        CONSTRAINT UQ_LedgerEntry_IdempotencyKey UNIQUE (IdempotencyKey)
    );
END
GO
