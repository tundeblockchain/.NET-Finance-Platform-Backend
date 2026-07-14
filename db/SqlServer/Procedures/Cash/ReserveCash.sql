/*
  Procedure : dbo.ReserveCash
  Purpose   : Reserves available cash (Settled - Reserved) while holding the lock. Creates a CashReservation and increases Reserved. Idempotent by IdempotencyKey. Fails when available cash is insufficient.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.ReserveCash
    @IdempotencyKey NVARCHAR(200),
    @AccountId UNIQUEIDENTIFIER,
    @Currency NVARCHAR(3),
    @Amount DECIMAL(18, 4),
    @TriggerId UNIQUEIDENTIFIER,
    @AllocationRequestId UNIQUEIDENTIFIER,
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Amount <= 0
    BEGIN
        THROW 50020, 'Reservation amount must be positive.', 1;
    END

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    BEGIN TRAN;

    IF EXISTS (SELECT 1 FROM dbo.CashReservation WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT b.*, CAST(1 AS BIT) AS AlreadyApplied
        FROM dbo.CashBalance b
        WHERE b.AccountId = @AccountId AND b.Currency = @Currency;
        SELECT * FROM dbo.CashReservation WHERE IdempotencyKey = @IdempotencyKey;
        COMMIT TRAN;
        RETURN;
    END

    DECLARE @Settled DECIMAL(18, 4);
    DECLARE @Reserved DECIMAL(18, 4);

    SELECT @Settled = Settled, @Reserved = Reserved
    FROM dbo.CashBalance WITH (UPDLOCK, ROWLOCK)
    WHERE AccountId = @AccountId
      AND Currency = @Currency
      AND IsLocked = 1
      AND LockedByTriggerId = @TriggerId
      AND (LockExpiresUtc IS NULL OR LockExpiresUtc > @Now);

    IF @Settled IS NULL
    BEGIN
        ROLLBACK TRAN;
        THROW 50021, 'Reservation requires an active cash lock owned by the trigger.', 1;
    END

    IF (@Settled - @Reserved) < @Amount
    BEGIN
        DECLARE @Available DECIMAL(18, 4) = @Settled - @Reserved;
        ROLLBACK TRAN;
        DECLARE @Msg NVARCHAR(200) = CONCAT(
            N'Insufficient available cash. Available=',
            FORMAT(@Available, '0.####'),
            N', requested=',
            FORMAT(@Amount, '0.####'),
            N'.');
        THROW 50022, @Msg, 1;
    END

    UPDATE dbo.CashBalance
    SET Reserved = Reserved + @Amount,
        DateModified = @Now,
        ChangedBy = @ChangedBy
    WHERE AccountId = @AccountId AND Currency = @Currency;

    INSERT INTO dbo.CashReservation
    (
        Id, AccountId, AllocationRequestId, TriggerId, Currency, Amount,
        IdempotencyKey, IsReleased, CreatedUtc, ReleasedUtc, DateModified, ChangedBy
    )
    VALUES
    (
        NEWID(), @AccountId, @AllocationRequestId, @TriggerId, @Currency, @Amount,
        @IdempotencyKey, 0, @Now, NULL, @Now, @ChangedBy
    );

    COMMIT TRAN;

    SELECT b.*, CAST(0 AS BIT) AS AlreadyApplied
    FROM dbo.CashBalance b
    WHERE b.AccountId = @AccountId AND b.Currency = @Currency;
    SELECT * FROM dbo.CashReservation WHERE IdempotencyKey = @IdempotencyKey;
END
GO
