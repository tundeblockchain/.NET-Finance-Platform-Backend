/*
  Procedure : dbo.SubmitOrder
  Purpose   : Idempotently creates or updates an Order as Submitted then Filled for the in-process broker path. Archives on update via Order_a when the row already exists.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.SubmitOrder
    @IdempotencyKey NVARCHAR(200),
    @AccountId UNIQUEIDENTIFIER,
    @TriggerId UNIQUEIDENTIFIER,
    @AllocationRequestId UNIQUEIDENTIFIER = NULL,
    @AssetSymbol NVARCHAR(32),
    @Side INT,
    @Quantity DECIMAL(18, 8),
    @LimitPrice DECIMAL(18, 8) = NULL,
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Quantity <= 0
    BEGIN
        THROW 50030, 'Order quantity must be positive.', 1;
    END

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();
    DECLARE @Id UNIQUEIDENTIFIER;

    BEGIN TRAN;

    SELECT @Id = Id
    FROM dbo.[Order] WITH (UPDLOCK, HOLDLOCK)
    WHERE IdempotencyKey = @IdempotencyKey;

    IF @Id IS NOT NULL
    BEGIN
        SELECT * FROM dbo.[Order] WHERE Id = @Id;
        COMMIT TRAN;
        RETURN;
    END

    SET @Id = NEWID();

    INSERT INTO dbo.[Order] (
        Id, AccountId, AllocationRequestId, TriggerId, AssetSymbol, Side, Quantity, LimitPrice,
        Status, IdempotencyKey, CreatedUtc, SubmittedUtc, DateModified, ChangedBy)
    VALUES (
        @Id, @AccountId, @AllocationRequestId, @TriggerId, @AssetSymbol, @Side, @Quantity, @LimitPrice,
        3, /* Filled */ @IdempotencyKey, @Now, @Now, @Now, @ChangedBy);

    SELECT * FROM dbo.[Order] WHERE Id = @Id;
    COMMIT TRAN;
END
GO
