SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.AssetPrice', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetPrice
    (
        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AssetPrice PRIMARY KEY,
        AssetSymbol      NVARCHAR(32)     NOT NULL,
        Price            DECIMAL(18, 8)   NOT NULL,
        Currency         NVARCHAR(8)      NOT NULL,
        Source           INT              NOT NULL,
        Provider         NVARCHAR(64)     NOT NULL,
        OrderId          UNIQUEIDENTIFIER NULL,
        ExternalOrderId  NVARCHAR(100)    NULL,
        ObservedUtc      DATETIMEOFFSET   NOT NULL,
        DateModified     DATETIMEOFFSET   NOT NULL,
        ChangedBy        NVARCHAR(100)    NOT NULL
    );

    CREATE INDEX IX_AssetPrice_Symbol_Observed
        ON dbo.AssetPrice (AssetSymbol, ObservedUtc DESC);
END
GO
