/*
  Procedure : dbo.MarkOrderFilled
  Purpose   : Marks an Order as Filled and archives prior row.
  Dated     : 2026-07-14
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.MarkOrderFilled
    @Id UNIQUEIDENTIFIER,
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.[Order] WHERE Id = @Id)
    BEGIN
        THROW 50051, 'Order was not found.', 1;
    END

    INSERT INTO dbo.Order_a (
        Id, AccountId, AllocationRequestId, TriggerId, AssetSymbol, Side, Quantity, LimitPrice,
        Status, IdempotencyKey, CreatedUtc, SubmittedUtc, DateModified, ChangedBy)
    SELECT Id, AccountId, AllocationRequestId, TriggerId, AssetSymbol, Side, Quantity, LimitPrice,
        Status, IdempotencyKey, CreatedUtc, SubmittedUtc, DateModified, ChangedBy
    FROM dbo.[Order] WHERE Id = @Id;

    UPDATE dbo.[Order]
    SET Status = 3, /* Filled */
        DateModified = SYSUTCDATETIME(),
        ChangedBy = @ChangedBy
    WHERE Id = @Id;

    SELECT * FROM dbo.[Order] WHERE Id = @Id;
END
GO
