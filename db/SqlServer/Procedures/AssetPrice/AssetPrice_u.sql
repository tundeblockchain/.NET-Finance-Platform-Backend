/*
  Procedure : dbo.AssetPrice_u
  Purpose   : Upserts an AssetPrice observation. On update, archives the current row into AssetPrice_a.
  Dated     : 2026-07-14
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.AssetPrice_u
    @Id UNIQUEIDENTIFIER,
    @AssetSymbol NVARCHAR(32),
    @Price DECIMAL(18, 8),
    @Currency NVARCHAR(8),
    @Source INT,
    @Provider NVARCHAR(64),
    @OrderId UNIQUEIDENTIFIER = NULL,
    @ExternalOrderId NVARCHAR(100) = NULL,
    @ObservedUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.AssetPrice WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.AssetPrice_a (
            Id, AssetSymbol, Price, Currency, Source, Provider, OrderId, ExternalOrderId,
            ObservedUtc, DateModified, ChangedBy)
        SELECT
            Id, AssetSymbol, Price, Currency, Source, Provider, OrderId, ExternalOrderId,
            ObservedUtc, DateModified, ChangedBy
        FROM dbo.AssetPrice WHERE Id = @Id;

        UPDATE dbo.AssetPrice
        SET AssetSymbol = @AssetSymbol,
            Price = @Price,
            Currency = @Currency,
            Source = @Source,
            Provider = @Provider,
            OrderId = @OrderId,
            ExternalOrderId = @ExternalOrderId,
            ObservedUtc = @ObservedUtc,
            DateModified = SYSUTCDATETIME(),
            ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.AssetPrice (
            Id, AssetSymbol, Price, Currency, Source, Provider, OrderId, ExternalOrderId,
            ObservedUtc, DateModified, ChangedBy)
        VALUES (
            @Id, @AssetSymbol, @Price, @Currency, @Source, @Provider, @OrderId, @ExternalOrderId,
            @ObservedUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.AssetPrice WHERE Id = @Id;
END
GO
