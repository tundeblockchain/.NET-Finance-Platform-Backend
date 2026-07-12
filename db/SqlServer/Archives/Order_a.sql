SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.Order_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Order_a
    (
        ArchiveId            BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Order_a PRIMARY KEY,
        ArchivedUtc          DATETIMEOFFSET NOT NULL CONSTRAINT DF_Order_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id                   UNIQUEIDENTIFIER NOT NULL,
        AccountId            UNIQUEIDENTIFIER NOT NULL,
        AllocationRequestId  UNIQUEIDENTIFIER NULL,
        TriggerId            UNIQUEIDENTIFIER NOT NULL,
        AssetSymbol          NVARCHAR(32)     NOT NULL,
        Side                 INT              NOT NULL,
        Quantity             DECIMAL(18, 8)   NOT NULL,
        LimitPrice           DECIMAL(18, 8)   NULL,
        Status               INT              NOT NULL,
        IdempotencyKey       NVARCHAR(200)    NOT NULL,
        CreatedUtc           DATETIMEOFFSET   NOT NULL,
        SubmittedUtc         DATETIMEOFFSET   NULL,
        DateModified         DATETIMEOFFSET   NOT NULL,
        ChangedBy            NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_Order_a_Id ON dbo.Order_a (Id, ArchivedUtc);
END
GO
