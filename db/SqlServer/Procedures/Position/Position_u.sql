/*
  Procedure : dbo.Position_u
  Purpose   : Upserts a Position. On update, archives the current row into Position_a before applying changes. Sets DateModified and ChangedBy.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.Position_u
    @Id UNIQUEIDENTIFIER,
    @AccountId UNIQUEIDENTIFIER,
    @AssetSymbol NVARCHAR(32),
    @Quantity DECIMAL(18, 8),
    @AverageCost DECIMAL(18, 8),
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Position WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.Position_a (Id, AccountId, AssetSymbol, Quantity, AverageCost, DateModified, ChangedBy)
        SELECT Id, AccountId, AssetSymbol, Quantity, AverageCost, DateModified, ChangedBy
        FROM dbo.Position WHERE Id = @Id;

        UPDATE dbo.Position
        SET AccountId = @AccountId, AssetSymbol = @AssetSymbol, Quantity = @Quantity, AverageCost = @AverageCost,
            DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Position (Id, AccountId, AssetSymbol, Quantity, AverageCost, DateModified, ChangedBy)
        VALUES (@Id, @AccountId, @AssetSymbol, @Quantity, @AverageCost, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.Position WHERE Id = @Id;
END
GO
