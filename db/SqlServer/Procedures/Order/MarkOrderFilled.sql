/*
  Procedure : dbo.MarkOrderFilled
  Purpose   : Marks an Order as Filled with broker fill details and archives prior row.
  Dated     : 2026-07-23
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.MarkOrderFilled
    @Id UNIQUEIDENTIFIER,
    @FillPrice DECIMAL(18, 8) = NULL,
    @ExternalOrderId NVARCHAR(100) = NULL,
    @Provider NVARCHAR(64) = NULL,
    @FilledUtc DATETIMEOFFSET = NULL,
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.[Order] WHERE Id = @Id)
    BEGIN
        THROW 50051, 'Order was not found.', 1;
    END

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    -- Idempotent: already filled returns current row.
    IF EXISTS (SELECT 1 FROM dbo.[Order] WHERE Id = @Id AND Status = 3 /* Filled */)
    BEGIN
        SELECT * FROM dbo.[Order] WHERE Id = @Id;
        RETURN;
    END

    INSERT INTO dbo.Order_a (
        Id, AccountId, AllocationRequestId, TriggerId, AssetSymbol, Side, Quantity, LimitPrice,
        FillPrice, ExternalOrderId, Provider, Status, IdempotencyKey, CreatedUtc, SubmittedUtc, FilledUtc,
        DateModified, ChangedBy)
    SELECT Id, AccountId, AllocationRequestId, TriggerId, AssetSymbol, Side, Quantity, LimitPrice,
        FillPrice, ExternalOrderId, Provider, Status, IdempotencyKey, CreatedUtc, SubmittedUtc, FilledUtc,
        DateModified, ChangedBy
    FROM dbo.[Order] WHERE Id = @Id;

    UPDATE dbo.[Order]
    SET Status = 3, /* Filled */
        FillPrice = @FillPrice,
        ExternalOrderId = @ExternalOrderId,
        Provider = @Provider,
        FilledUtc = COALESCE(@FilledUtc, @Now),
        DateModified = @Now,
        ChangedBy = @ChangedBy
    WHERE Id = @Id;

    SELECT * FROM dbo.[Order] WHERE Id = @Id;
END
GO
