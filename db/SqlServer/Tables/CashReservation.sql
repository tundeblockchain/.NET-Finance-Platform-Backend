SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.CashReservation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashReservation
    (
        Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CashReservation PRIMARY KEY,
        AccountId            UNIQUEIDENTIFIER NOT NULL,
        AllocationRequestId  UNIQUEIDENTIFIER NOT NULL,
        TriggerId            UNIQUEIDENTIFIER NOT NULL,
        Currency             NVARCHAR(3)      NOT NULL,
        Amount               DECIMAL(18, 4)   NOT NULL,
        IdempotencyKey       NVARCHAR(200)    NOT NULL,
        IsReleased           BIT              NOT NULL CONSTRAINT DF_CashReservation_IsReleased DEFAULT (0),
        CreatedUtc           DATETIMEOFFSET   NOT NULL,
        ReleasedUtc          DATETIMEOFFSET   NULL,
        DateModified         DATETIMEOFFSET   NOT NULL,
        ChangedBy            NVARCHAR(100)    NOT NULL,
        CONSTRAINT UQ_CashReservation_IdempotencyKey UNIQUE (IdempotencyKey)
    );
END
GO
