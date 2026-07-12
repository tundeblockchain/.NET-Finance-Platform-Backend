SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.AllocationRequest_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllocationRequest_a
    (
        ArchiveId        BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_AllocationRequest_a PRIMARY KEY,
        ArchivedUtc      DATETIMEOFFSET NOT NULL CONSTRAINT DF_AllocationRequest_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id               UNIQUEIDENTIFIER NOT NULL,
        CustomerId       UNIQUEIDENTIFIER NOT NULL,
        AccountId        UNIQUEIDENTIFIER NOT NULL,
        IdempotencyKey   NVARCHAR(200)    NOT NULL,
        Status           INT              NOT NULL,
        Amount           DECIMAL(18, 4)   NOT NULL,
        Currency         NVARCHAR(3)      NOT NULL,
        RootWorkflowId   UNIQUEIDENTIFIER NOT NULL,
        CreatedUtc       DATETIMEOFFSET   NOT NULL,
        CompletedUtc     DATETIMEOFFSET   NULL,
        DateModified     DATETIMEOFFSET   NOT NULL,
        ChangedBy        NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_AllocationRequest_a_Id ON dbo.AllocationRequest_a (Id, ArchivedUtc);
END
GO
