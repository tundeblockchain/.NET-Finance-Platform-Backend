SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.AssetPrice_a', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssetPrice_a
    (
        ArchiveId        BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_AssetPrice_a PRIMARY KEY,
        ArchivedUtc      DATETIMEOFFSET NOT NULL CONSTRAINT DF_AssetPrice_a_ArchivedUtc DEFAULT (SYSUTCDATETIME()),
        Id               UNIQUEIDENTIFIER NOT NULL,
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
    CREATE INDEX IX_AssetPrice_a_Id ON dbo.AssetPrice_a (Id, ArchivedUtc);
END
GO
