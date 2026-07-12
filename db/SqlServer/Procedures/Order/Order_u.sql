/*
  Procedure : dbo.Order_u
  Purpose   : Upserts an Order. On update, archives the current row into Order_a before applying changes. Sets DateModified and ChangedBy.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.Order_u
    @Id UNIQUEIDENTIFIER,
    @AccountId UNIQUEIDENTIFIER,
    @AllocationRequestId UNIQUEIDENTIFIER,
    @TriggerId UNIQUEIDENTIFIER,
    @AssetSymbol NVARCHAR(32),
    @Side INT,
    @Quantity DECIMAL(18, 8),
    @LimitPrice DECIMAL(18, 8),
    @Status INT,
    @IdempotencyKey NVARCHAR(200),
    @CreatedUtc DATETIMEOFFSET,
    @SubmittedUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.[Order] WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.Order_a (Id, AccountId, AllocationRequestId, TriggerId, AssetSymbol, Side, Quantity, LimitPrice, Status, IdempotencyKey, CreatedUtc, SubmittedUtc, DateModified, ChangedBy)
        SELECT Id, AccountId, AllocationRequestId, TriggerId, AssetSymbol, Side, Quantity, LimitPrice, Status, IdempotencyKey, CreatedUtc, SubmittedUtc, DateModified, ChangedBy
        FROM dbo.[Order] WHERE Id = @Id;

        UPDATE dbo.[Order]
        SET AccountId = @AccountId, AllocationRequestId = @AllocationRequestId, TriggerId = @TriggerId, AssetSymbol = @AssetSymbol,
            Side = @Side, Quantity = @Quantity, LimitPrice = @LimitPrice, Status = @Status, IdempotencyKey = @IdempotencyKey,
            CreatedUtc = @CreatedUtc, SubmittedUtc = @SubmittedUtc, DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.[Order] (Id, AccountId, AllocationRequestId, TriggerId, AssetSymbol, Side, Quantity, LimitPrice, Status, IdempotencyKey, CreatedUtc, SubmittedUtc, DateModified, ChangedBy)
        VALUES (@Id, @AccountId, @AllocationRequestId, @TriggerId, @AssetSymbol, @Side, @Quantity, @LimitPrice, @Status, @IdempotencyKey, @CreatedUtc, @SubmittedUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.[Order] WHERE Id = @Id;
END
GO
