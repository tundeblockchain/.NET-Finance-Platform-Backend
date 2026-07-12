SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.Position', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Position
    (
        Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Position PRIMARY KEY,
        AccountId     UNIQUEIDENTIFIER NOT NULL,
        AssetSymbol   NVARCHAR(32)     NOT NULL,
        Quantity      DECIMAL(18, 8)   NOT NULL,
        AverageCost   DECIMAL(18, 8)   NOT NULL,
        DateModified  DATETIMEOFFSET   NOT NULL,
        ChangedBy     NVARCHAR(100)    NOT NULL,
        CONSTRAINT UQ_Position_Account_Asset UNIQUE (AccountId, AssetSymbol)
    );
END
GO
