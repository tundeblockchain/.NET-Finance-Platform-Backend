/*
  Procedure : dbo.ApplyPositionSell
  Purpose   : Decreases position quantity when available. Idempotent via BrokerMutation.IdempotencyKey.
  Dated     : 2026-07-14
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.ApplyPositionSell
    @IdempotencyKey NVARCHAR(200),
    @AccountId UNIQUEIDENTIFIER,
    @AssetSymbol NVARCHAR(32),
    @Quantity DECIMAL(18, 8),
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Quantity <= 0
    BEGIN
        THROW 50061, 'Sell quantity must be positive.', 1;
    END

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();
    DECLARE @Current DECIMAL(18, 8);

    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.BrokerMutation WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT ISNULL((
            SELECT Quantity FROM dbo.Position
            WHERE AccountId = @AccountId AND AssetSymbol = @AssetSymbol), 0) AS Quantity,
               CAST(1 AS BIT) AS AlreadyApplied;
        COMMIT TRAN;
        RETURN;
    END

    SELECT @Current = Quantity
    FROM dbo.Position WITH (UPDLOCK, ROWLOCK)
    WHERE AccountId = @AccountId AND AssetSymbol = @AssetSymbol;

    SET @Current = ISNULL(@Current, 0);

    IF @Current < @Quantity
    BEGIN
        ROLLBACK TRAN;
        DECLARE @Msg NVARCHAR(200) = CONCAT(
            N'Insufficient position for ', @AssetSymbol,
            N'. Available=', FORMAT(@Current, '0.########'),
            N', requested=', FORMAT(@Quantity, '0.########'), N'.');
        THROW 50062, @Msg, 1;
    END

    INSERT INTO dbo.Position_a (Id, AccountId, AssetSymbol, Quantity, AverageCost, DateModified, ChangedBy)
    SELECT Id, AccountId, AssetSymbol, Quantity, AverageCost, DateModified, ChangedBy
    FROM dbo.Position WHERE AccountId = @AccountId AND AssetSymbol = @AssetSymbol;

    UPDATE dbo.Position
    SET Quantity = Quantity - @Quantity,
        DateModified = @Now,
        ChangedBy = @ChangedBy
    WHERE AccountId = @AccountId AND AssetSymbol = @AssetSymbol;

    INSERT INTO dbo.BrokerMutation (IdempotencyKey, TriggerId, CreatedUtc)
    VALUES (@IdempotencyKey, NULL, @Now);

    COMMIT TRAN;

    SELECT Quantity, CAST(0 AS BIT) AS AlreadyApplied
    FROM dbo.Position
    WHERE AccountId = @AccountId AND AssetSymbol = @AssetSymbol;
END
GO
