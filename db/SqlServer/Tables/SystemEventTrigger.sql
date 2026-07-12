SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.SystemEventTrigger', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemEventTrigger
    (
        Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SystemEventTrigger PRIMARY KEY,
        TriggerCode          INT              NOT NULL,
        QueueName            NVARCHAR(64)     NOT NULL,
        Status               INT              NOT NULL,
        PayloadJson          NVARCHAR(MAX)    NOT NULL,
        ResultJson           NVARCHAR(MAX)    NULL,
        RootWorkflowId       UNIQUEIDENTIFIER NOT NULL,
        CorrelationId        UNIQUEIDENTIFIER NOT NULL,
        ParentTriggerId      UNIQUEIDENTIFIER NULL,
        SourceTriggerId      UNIQUEIDENTIFIER NULL,
        AllocationRequestId  UNIQUEIDENTIFIER NULL,
        ExternalId           UNIQUEIDENTIFIER NULL,
        ExternalType         INT              NULL,
        SourceComponent      NVARCHAR(100)    NOT NULL,
        TargetComponent      NVARCHAR(100)    NOT NULL,
        IdempotencyKey       NVARCHAR(200)    NOT NULL,
        AttemptCount         INT              NOT NULL CONSTRAINT DF_SystemEventTrigger_AttemptCount DEFAULT (0),
        NextAttemptUtc       DATETIMEOFFSET   NULL,
        LastError            NVARCHAR(2000)   NULL,
        CreatedUtc           DATETIMEOFFSET   NOT NULL,
        CompletedUtc         DATETIMEOFFSET   NULL,
        DateModified         DATETIMEOFFSET   NOT NULL,
        ChangedBy            NVARCHAR(100)    NOT NULL,
        CONSTRAINT UQ_SystemEventTrigger_IdempotencyKey UNIQUE (IdempotencyKey)
    );

    CREATE INDEX IX_SystemEventTrigger_Claim
        ON dbo.SystemEventTrigger (QueueName, Status, NextAttemptUtc, CreatedUtc);
END
GO
