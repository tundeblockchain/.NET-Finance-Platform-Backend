SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.CashReservation_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashReservation_a
    (
        ArchiveId            BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_CashReservation_a PRIMARY KEY,
        ArchivedUtc          DATETIMEOFFSET NOT NULL CONSTRAINT DF_CashReservation_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id                   UNIQUEIDENTIFIER NOT NULL,
        AccountId            UNIQUEIDENTIFIER NOT NULL,
        AllocationRequestId  UNIQUEIDENTIFIER NOT NULL,
        TriggerId            UNIQUEIDENTIFIER NOT NULL,
        Currency             NVARCHAR(3)      NOT NULL,
        Amount               DECIMAL(18, 4)   NOT NULL,
        IdempotencyKey       NVARCHAR(200)    NOT NULL,
        IsReleased           BIT              NOT NULL,
        CreatedUtc           DATETIMEOFFSET   NOT NULL,
        ReleasedUtc          DATETIMEOFFSET   NULL,
        DateModified         DATETIMEOFFSET   NOT NULL,
        ChangedBy            NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_CashReservation_a_Id ON dbo.CashReservation_a (Id, ArchivedUtc);
END
GO
