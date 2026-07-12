SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.AllocationRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AllocationRequest
    (
        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AllocationRequest PRIMARY KEY,
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
        ChangedBy        NVARCHAR(100)    NOT NULL,
        CONSTRAINT UQ_AllocationRequest_IdempotencyKey UNIQUE (IdempotencyKey)
    );
END
GO
