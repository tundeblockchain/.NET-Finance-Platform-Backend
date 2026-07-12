SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.CashBalance', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashBalance
    (
        Id                    UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CashBalance PRIMARY KEY,
        AccountId             UNIQUEIDENTIFIER NOT NULL,
        Currency              NVARCHAR(3)      NOT NULL,
        Settled               DECIMAL(18, 4)   NOT NULL CONSTRAINT DF_CashBalance_Settled DEFAULT (0),
        Reserved              DECIMAL(18, 4)   NOT NULL CONSTRAINT DF_CashBalance_Reserved DEFAULT (0),
        IsLocked              BIT              NOT NULL CONSTRAINT DF_CashBalance_IsLocked DEFAULT (0),
        LockedByAllocationId  UNIQUEIDENTIFIER NULL,
        LockedByTriggerId     UNIQUEIDENTIFIER NULL,
        LockAcquiredUtc       DATETIMEOFFSET   NULL,
        LockExpiresUtc        DATETIMEOFFSET   NULL,
        DateModified          DATETIMEOFFSET   NOT NULL,
        ChangedBy             NVARCHAR(100)    NOT NULL,
        CONSTRAINT UQ_CashBalance_Account_Currency UNIQUE (AccountId, Currency)
    );
END
GO
