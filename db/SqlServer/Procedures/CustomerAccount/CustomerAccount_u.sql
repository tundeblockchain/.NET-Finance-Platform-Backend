/*
  Procedure : dbo.CustomerAccount_u
  Purpose   : Upserts a CustomerAccount. On update, archives into CustomerAccount_a. Sets DateModified and ChangedBy.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.CustomerAccount_u
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

    IF EXISTS (SELECT 1 FROM dbo.CustomerAccount WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.CustomerAccount_a (Id, CustomerId, Currency, Settled, Reserved, IsLocked, LockedByTriggerId, LockExpiresUtc, CreatedUtc, DateModified, ChangedBy)
        SELECT Id, CustomerId, Currency, Settled, Reserved, IsLocked, LockedByTriggerId, LockExpiresUtc, CreatedUtc, DateModified, ChangedBy
        FROM dbo.CustomerAccount WHERE Id = @Id;

        UPDATE dbo.CustomerAccount
        SET CustomerId = @CustomerId, Currency = @Currency, Settled = @Settled, Reserved = @Reserved,
            IsLocked = @IsLocked, LockedByTriggerId = @LockedByTriggerId, LockExpiresUtc = @LockExpiresUtc,
            CreatedUtc = @CreatedUtc, DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.CustomerAccount (Id, CustomerId, Currency, Settled, Reserved, IsLocked, LockedByTriggerId, LockExpiresUtc, CreatedUtc, DateModified, ChangedBy)
        VALUES (@Id, @CustomerId, @Currency, @Settled, @Reserved, @IsLocked, @LockedByTriggerId, @LockExpiresUtc, @CreatedUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.CustomerAccount WHERE Id = @Id;
END
GO
