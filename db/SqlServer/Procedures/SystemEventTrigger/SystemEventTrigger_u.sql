SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.SystemEventTrigger_u
    @Id UNIQUEIDENTIFIER,
    @TriggerCode INT,
    @QueueName NVARCHAR(64),
    @Status INT,
    @PayloadJson NVARCHAR(MAX),
    @ResultJson NVARCHAR(MAX),
    @RootWorkflowId UNIQUEIDENTIFIER,
    @CorrelationId UNIQUEIDENTIFIER,
    @ParentTriggerId UNIQUEIDENTIFIER,
    @SourceTriggerId UNIQUEIDENTIFIER,
    @AllocationRequestId UNIQUEIDENTIFIER,
    @ExternalId UNIQUEIDENTIFIER,
    @ExternalType INT,
    @SourceComponent NVARCHAR(100),
    @TargetComponent NVARCHAR(100),
    @IdempotencyKey NVARCHAR(200),
    @AttemptCount INT,
    @NextAttemptUtc DATETIMEOFFSET,
    @LastError NVARCHAR(2000),
    @CreatedUtc DATETIMEOFFSET,
    @CompletedUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.SystemEventTrigger WHERE Id = @Id)
    BEGIN
        UPDATE dbo.SystemEventTrigger
        SET TriggerCode = @TriggerCode, QueueName = @QueueName, Status = @Status, PayloadJson = @PayloadJson,
            ResultJson = @ResultJson, RootWorkflowId = @RootWorkflowId, CorrelationId = @CorrelationId,
            ParentTriggerId = @ParentTriggerId, SourceTriggerId = @SourceTriggerId,
            AllocationRequestId = @AllocationRequestId, ExternalId = @ExternalId, ExternalType = @ExternalType,
            SourceComponent = @SourceComponent, TargetComponent = @TargetComponent, IdempotencyKey = @IdempotencyKey,
            AttemptCount = @AttemptCount, NextAttemptUtc = @NextAttemptUtc, LastError = @LastError,
            CreatedUtc = @CreatedUtc, CompletedUtc = @CompletedUtc, DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.SystemEventTrigger
            (Id, TriggerCode, QueueName, Status, PayloadJson, ResultJson, RootWorkflowId, CorrelationId, ParentTriggerId,
             SourceTriggerId, AllocationRequestId, ExternalId, ExternalType, SourceComponent, TargetComponent, IdempotencyKey,
             AttemptCount, NextAttemptUtc, LastError, CreatedUtc, CompletedUtc, DateModified, ChangedBy)
        VALUES
            (@Id, @TriggerCode, @QueueName, @Status, @PayloadJson, @ResultJson, @RootWorkflowId, @CorrelationId, @ParentTriggerId,
             @SourceTriggerId, @AllocationRequestId, @ExternalId, @ExternalType, @SourceComponent, @TargetComponent, @IdempotencyKey,
             @AttemptCount, @NextAttemptUtc, @LastError, @CreatedUtc, @CompletedUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.SystemEventTrigger WHERE Id = @Id;
END
GO
