SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.CashBalance_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashBalance_a
    (
        ArchiveId             BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_CashBalance_a PRIMARY KEY,
        ArchivedUtc           DATETIMEOFFSET NOT NULL CONSTRAINT DF_CashBalance_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id                    UNIQUEIDENTIFIER NOT NULL,
        AccountId             UNIQUEIDENTIFIER NOT NULL,
        Currency              NVARCHAR(3)      NOT NULL,
        Settled               DECIMAL(18, 4)   NOT NULL,
        Reserved              DECIMAL(18, 4)   NOT NULL,
        IsLocked              BIT              NOT NULL,
        LockedByAllocationId  UNIQUEIDENTIFIER NULL,
        LockedByTriggerId     UNIQUEIDENTIFIER NULL,
        LockAcquiredUtc       DATETIMEOFFSET   NULL,
        LockExpiresUtc        DATETIMEOFFSET   NULL,
        DateModified          DATETIMEOFFSET   NOT NULL,
        ChangedBy             NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_CashBalance_a_Id ON dbo.CashBalance_a (Id, ArchivedUtc);
END
GO
