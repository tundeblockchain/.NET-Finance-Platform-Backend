SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.BrokerMutation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BrokerMutation
    (
        IdempotencyKey  NVARCHAR(200)    NOT NULL CONSTRAINT PK_BrokerMutation PRIMARY KEY,
        TriggerId       UNIQUEIDENTIFIER NULL,
        CreatedUtc      DATETIMEOFFSET   NOT NULL CONSTRAINT DF_BrokerMutation_CreatedUtc DEFAULT (SYSUTCDATETIME())
    );
END
GO
