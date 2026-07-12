SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.AllocationRequest_u
    @Id UNIQUEIDENTIFIER,
    @CustomerId UNIQUEIDENTIFIER,
    @AccountId UNIQUEIDENTIFIER,
    @IdempotencyKey NVARCHAR(200),
    @Status INT,
    @Amount DECIMAL(18, 4),
    @Currency NVARCHAR(3),
    @RootWorkflowId UNIQUEIDENTIFIER,
    @CreatedUtc DATETIMEOFFSET,
    @CompletedUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.AllocationRequest WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.AllocationRequest_a (Id, CustomerId, AccountId, IdempotencyKey, Status, Amount, Currency, RootWorkflowId, CreatedUtc, CompletedUtc, DateModified, ChangedBy)
        SELECT Id, CustomerId, AccountId, IdempotencyKey, Status, Amount, Currency, RootWorkflowId, CreatedUtc, CompletedUtc, DateModified, ChangedBy
        FROM dbo.AllocationRequest WHERE Id = @Id;

        UPDATE dbo.AllocationRequest
        SET CustomerId = @CustomerId, AccountId = @AccountId, IdempotencyKey = @IdempotencyKey, Status = @Status,
            Amount = @Amount, Currency = @Currency, RootWorkflowId = @RootWorkflowId, CreatedUtc = @CreatedUtc,
            CompletedUtc = @CompletedUtc, DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.AllocationRequest (Id, CustomerId, AccountId, IdempotencyKey, Status, Amount, Currency, RootWorkflowId, CreatedUtc, CompletedUtc, DateModified, ChangedBy)
        VALUES (@Id, @CustomerId, @AccountId, @IdempotencyKey, @Status, @Amount, @Currency, @RootWorkflowId, @CreatedUtc, @CompletedUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.AllocationRequest WHERE Id = @Id;
END
GO
