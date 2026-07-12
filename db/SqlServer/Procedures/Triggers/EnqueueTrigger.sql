/*
  Procedure : dbo.EnqueueTrigger
  Purpose   : Enqueues a pending trigger idempotently by IdempotencyKey. Returns the existing trigger when the key already exists. Sets ChangedBy to broker.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.EnqueueTrigger
    @Id UNIQUEIDENTIFIER,
    @TriggerCode INT,
    @QueueName NVARCHAR(64),
    @PayloadJson NVARCHAR(MAX),
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
    @NextAttemptUtc DATETIMEOFFSET
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.SystemEventTrigger WITH (UPDLOCK, HOLDLOCK)
        WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

        INSERT INTO dbo.SystemEventTrigger
            (Id, TriggerCode, QueueName, Status, PayloadJson, ResultJson, RootWorkflowId, CorrelationId, ParentTriggerId,
             SourceTriggerId, AllocationRequestId, ExternalId, ExternalType, SourceComponent, TargetComponent, IdempotencyKey,
             AttemptCount, NextAttemptUtc, LastError, CreatedUtc, CompletedUtc, DateModified, ChangedBy)
        VALUES
            (@Id, @TriggerCode, @QueueName, 0, @PayloadJson, NULL, @RootWorkflowId, @CorrelationId, @ParentTriggerId,
             @SourceTriggerId, @AllocationRequestId, @ExternalId, @ExternalType, @SourceComponent, @TargetComponent, @IdempotencyKey,
             0, COALESCE(@NextAttemptUtc, @Now), NULL, @Now, NULL, @Now, N'broker');
    END

    COMMIT TRANSACTION;

    SELECT * FROM dbo.SystemEventTrigger WHERE IdempotencyKey = @IdempotencyKey;
END
GO
