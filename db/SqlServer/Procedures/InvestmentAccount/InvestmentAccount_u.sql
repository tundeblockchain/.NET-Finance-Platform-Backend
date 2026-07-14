/*
  Procedure : dbo.InvestmentAccount_u
  Purpose   : Upserts an InvestmentAccount. On update, archives into InvestmentAccount_a.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.InvestmentAccount_u
    @Id UNIQUEIDENTIFIER,
    @CustomerId INT,
    @TradingAccountId UNIQUEIDENTIFIER,
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

    IF EXISTS (SELECT 1 FROM dbo.InvestmentAccount WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.InvestmentAccount_a (
            Id, CustomerId, TradingAccountId, Currency, Settled, Reserved, IsLocked,
            LockedByTriggerId, LockExpiresUtc, CreatedUtc, DateModified, ChangedBy)
        SELECT Id, CustomerId, TradingAccountId, Currency, Settled, Reserved, IsLocked,
            LockedByTriggerId, LockExpiresUtc, CreatedUtc, DateModified, ChangedBy
        FROM dbo.InvestmentAccount WHERE Id = @Id;

        UPDATE dbo.InvestmentAccount
        SET CustomerId = @CustomerId,
            TradingAccountId = @TradingAccountId,
            Currency = @Currency,
            Settled = @Settled,
            Reserved = @Reserved,
            IsLocked = @IsLocked,
            LockedByTriggerId = @LockedByTriggerId,
            LockExpiresUtc = @LockExpiresUtc,
            CreatedUtc = @CreatedUtc,
            DateModified = SYSUTCDATETIME(),
            ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.InvestmentAccount (
            Id, CustomerId, TradingAccountId, Currency, Settled, Reserved, IsLocked,
            LockedByTriggerId, LockExpiresUtc, CreatedUtc, DateModified, ChangedBy)
        VALUES (
            @Id, @CustomerId, @TradingAccountId, @Currency, @Settled, @Reserved, @IsLocked,
            @LockedByTriggerId, @LockExpiresUtc, @CreatedUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.InvestmentAccount WHERE Id = @Id;
END
GO
