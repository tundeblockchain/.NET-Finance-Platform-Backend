/*
  Procedure : dbo.ApplyPositionBuy
  Purpose   : Increases position quantity. Idempotent via BrokerMutation.IdempotencyKey.
  Dated     : 2026-07-14
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.ApplyPositionBuy
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
        THROW 50060, 'Buy quantity must be positive.', 1;
    END

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.BrokerMutation WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT *, CAST(1 AS BIT) AS AlreadyApplied
        FROM dbo.Position
        WHERE AccountId = @AccountId AND AssetSymbol = @AssetSymbol;
        COMMIT TRAN;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Position WHERE AccountId = @AccountId AND AssetSymbol = @AssetSymbol)
    BEGIN
        INSERT INTO dbo.Position_a (Id, AccountId, AssetSymbol, Quantity, AverageCost, DateModified, ChangedBy)
        SELECT Id, AccountId, AssetSymbol, Quantity, AverageCost, DateModified, ChangedBy
        FROM dbo.Position WHERE AccountId = @AccountId AND AssetSymbol = @AssetSymbol;

        UPDATE dbo.Position
        SET Quantity = Quantity + @Quantity,
            DateModified = @Now,
            ChangedBy = @ChangedBy
        WHERE AccountId = @AccountId AND AssetSymbol = @AssetSymbol;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Position (Id, AccountId, AssetSymbol, Quantity, AverageCost, DateModified, ChangedBy)
        VALUES (NEWID(), @AccountId, @AssetSymbol, @Quantity, 0, @Now, @ChangedBy);
    END

    INSERT INTO dbo.BrokerMutation (IdempotencyKey, TriggerId, CreatedUtc)
    VALUES (@IdempotencyKey, NULL, @Now);

    COMMIT TRAN;

    SELECT *, CAST(0 AS BIT) AS AlreadyApplied
    FROM dbo.Position
    WHERE AccountId = @AccountId AND AssetSymbol = @AssetSymbol;
END
GO
