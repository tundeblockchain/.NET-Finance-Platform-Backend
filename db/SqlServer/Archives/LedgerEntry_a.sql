SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.LedgerEntry_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LedgerEntry_a
    (
        ArchiveId            BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_LedgerEntry_a PRIMARY KEY,
        ArchivedUtc          DATETIMEOFFSET NOT NULL CONSTRAINT DF_LedgerEntry_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id                   UNIQUEIDENTIFIER NOT NULL,
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
        ChangedBy            NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_LedgerEntry_a_Id ON dbo.LedgerEntry_a (Id, ArchivedUtc);
END
GO
