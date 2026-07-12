SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.Position_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Position_a
    (
        ArchiveId     BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Position_a PRIMARY KEY,
        ArchivedUtc   DATETIMEOFFSET NOT NULL CONSTRAINT DF_Position_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id            UNIQUEIDENTIFIER NOT NULL,
        AccountId     UNIQUEIDENTIFIER NOT NULL,
        AssetSymbol   NVARCHAR(32)     NOT NULL,
        Quantity      DECIMAL(18, 8)   NOT NULL,
        AverageCost   DECIMAL(18, 8)   NOT NULL,
        DateModified  DATETIMEOFFSET   NOT NULL,
        ChangedBy     NVARCHAR(100)    NOT NULL
    );
    CREATE INDEX IX_Position_a_Id ON dbo.Position_a (Id, ArchivedUtc);
END
GO
