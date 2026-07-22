/*
  Procedure : dbo.SubmitOrder
  Purpose   : Idempotently creates an Order as Filled for the in-process / broker-backed path.
  Dated     : 2026-07-14
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
    @FillPrice DECIMAL(18, 8) = NULL,
    @ExternalOrderId NVARCHAR(100) = NULL,
    @Provider NVARCHAR(64) = NULL,
    @FilledUtc DATETIMEOFFSET = NULL,
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
        SELECT *, CAST(1 AS BIT) AS AlreadyApplied FROM dbo.[Order] WHERE Id = @Id;
        COMMIT TRAN;
        RETURN;
    END

    SET @Id = NEWID();

    INSERT INTO dbo.[Order] (
        Id, AccountId, AllocationRequestId, TriggerId, AssetSymbol, Side, Quantity, LimitPrice,
        FillPrice, ExternalOrderId, Provider, Status, IdempotencyKey, CreatedUtc, SubmittedUtc, FilledUtc,
        DateModified, ChangedBy)
    VALUES (
        @Id, @AccountId, @AllocationRequestId, @TriggerId, @AssetSymbol, @Side, @Quantity, @LimitPrice,
        @FillPrice, @ExternalOrderId, @Provider, 3, /* Filled */ @IdempotencyKey, @Now, @Now,
        COALESCE(@FilledUtc, @Now), @Now, @ChangedBy);

    SELECT *, CAST(0 AS BIT) AS AlreadyApplied FROM dbo.[Order] WHERE Id = @Id;
    COMMIT TRAN;
END
GO
