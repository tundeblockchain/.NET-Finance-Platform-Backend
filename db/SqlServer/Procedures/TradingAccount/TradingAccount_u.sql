/*
  Procedure : dbo.TradingAccount_u
  Purpose   : Upserts a TradingAccount. On update, archives into TradingAccount_a. Sets DateModified and ChangedBy.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.TradingAccount_u
    @Id UNIQUEIDENTIFIER,
    @CustomerId INT,
    @Currency NVARCHAR(3),
    @Settled DECIMAL(18,4),
    @Reserved DECIMAL(18,4),
    @IsLocked BIT,
    @LockedByTriggerId UNIQUEIDENTIFIER = NULL,
    @LockExpiresUtc DATETIMEOFFSET = NULL,
    @CreatedUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.TradingAccount WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.TradingAccount_a (Id, CustomerId, Currency, Settled, Reserved, IsLocked, LockedByTriggerId, LockExpiresUtc, CreatedUtc, DateModified, ChangedBy)
        SELECT Id, CustomerId, Currency, Settled, Reserved, IsLocked, LockedByTriggerId, LockExpiresUtc, CreatedUtc, DateModified, ChangedBy
        FROM dbo.TradingAccount WHERE Id = @Id;

        UPDATE dbo.TradingAccount
        SET CustomerId = @CustomerId, Currency = @Currency, Settled = @Settled, Reserved = @Reserved,
            IsLocked = @IsLocked, LockedByTriggerId = @LockedByTriggerId, LockExpiresUtc = @LockExpiresUtc,
            CreatedUtc = @CreatedUtc, DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.TradingAccount (Id, CustomerId, Currency, Settled, Reserved, IsLocked, LockedByTriggerId, LockExpiresUtc, CreatedUtc, DateModified, ChangedBy)
        VALUES (@Id, @CustomerId, @Currency, @Settled, @Reserved, @IsLocked, @LockedByTriggerId, @LockExpiresUtc, @CreatedUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.TradingAccount WHERE Id = @Id;
END
GO
