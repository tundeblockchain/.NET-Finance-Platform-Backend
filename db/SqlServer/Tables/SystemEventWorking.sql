SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.SystemEventWorking', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemEventWorking
    (
        TriggerId          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SystemEventWorking PRIMARY KEY,
        WorkerInstanceId   NVARCHAR(200)    NOT NULL,
        QueueName          NVARCHAR(64)     NOT NULL,
        ClaimedUtc         DATETIMEOFFSET   NOT NULL,
        HeartbeatUtc       DATETIMEOFFSET   NOT NULL,
        LeaseExpiresUtc    DATETIMEOFFSET   NOT NULL,
        DateModified       DATETIMEOFFSET   NOT NULL,
        ChangedBy          NVARCHAR(100)    NOT NULL
    );

    CREATE INDEX IX_SystemEventWorking_Lease
        ON dbo.SystemEventWorking (LeaseExpiresUtc);
END
GO
