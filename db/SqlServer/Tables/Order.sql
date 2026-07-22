SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.[Order]', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Order]
    (
        Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Order PRIMARY KEY,
        AccountId            UNIQUEIDENTIFIER NOT NULL,
        AllocationRequestId  UNIQUEIDENTIFIER NULL,
        TriggerId            UNIQUEIDENTIFIER NOT NULL,
        AssetSymbol          NVARCHAR(32)     NOT NULL,
        Side                 INT              NOT NULL,
        Quantity             DECIMAL(18, 8)   NOT NULL,
        LimitPrice           DECIMAL(18, 8)   NULL,
        FillPrice            DECIMAL(18, 8)   NULL,
        ExternalOrderId      NVARCHAR(100)    NULL,
        Provider             NVARCHAR(64)     NULL,
        Status               INT              NOT NULL,
        IdempotencyKey       NVARCHAR(200)    NOT NULL,
        CreatedUtc           DATETIMEOFFSET   NOT NULL,
        SubmittedUtc         DATETIMEOFFSET   NULL,
        FilledUtc            DATETIMEOFFSET   NULL,
        DateModified         DATETIMEOFFSET   NOT NULL,
        ChangedBy            NVARCHAR(100)    NOT NULL,
        CONSTRAINT UQ_Order_IdempotencyKey UNIQUE (IdempotencyKey)
    );
END
GO

IF COL_LENGTH(N'dbo.Order', N'FillPrice') IS NULL
    ALTER TABLE dbo.[Order] ADD FillPrice DECIMAL(18, 8) NULL;
GO
IF COL_LENGTH(N'dbo.Order', N'ExternalOrderId') IS NULL
    ALTER TABLE dbo.[Order] ADD ExternalOrderId NVARCHAR(100) NULL;
GO
IF COL_LENGTH(N'dbo.Order', N'Provider') IS NULL
    ALTER TABLE dbo.[Order] ADD Provider NVARCHAR(64) NULL;
GO
IF COL_LENGTH(N'dbo.Order', N'FilledUtc') IS NULL
    ALTER TABLE dbo.[Order] ADD FilledUtc DATETIMEOFFSET NULL;
GO
