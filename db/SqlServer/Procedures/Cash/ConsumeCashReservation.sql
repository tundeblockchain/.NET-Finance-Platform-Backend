/*
  Procedure : dbo.ConsumeCashReservation
  Purpose   : Consumes a reservation: decreases both Settled and Reserved, then marks the reservation released. Requires an active lock owned by TriggerId. Idempotent when already released.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.ConsumeCashReservation
    @IdempotencyKey NVARCHAR(200),
    @TriggerId UNIQUEIDENTIFIER,
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();
    DECLARE @AccountId UNIQUEIDENTIFIER;
    DECLARE @Currency NVARCHAR(3);
    DECLARE @Amount DECIMAL(18, 4);
    DECLARE @IsReleased BIT;

    BEGIN TRAN;

    SELECT
        @AccountId = AccountId,
        @Currency = Currency,
        @Amount = Amount,
        @IsReleased = IsReleased
    FROM dbo.CashReservation WITH (UPDLOCK, ROWLOCK)
    WHERE IdempotencyKey = @IdempotencyKey;

    IF @AccountId IS NULL
    BEGIN
        ROLLBACK TRAN;
        THROW 50040, 'Reservation was not found.', 1;
    END

    IF NOT EXISTS (
        SELECT 1 FROM dbo.CashBalance
        WHERE AccountId = @AccountId AND Currency = @Currency
          AND IsLocked = 1 AND LockedByTriggerId = @TriggerId
          AND (LockExpiresUtc IS NULL OR LockExpiresUtc > @Now)
    )
    BEGIN
        ROLLBACK TRAN;
        THROW 50041, 'Consume reservation requires an active cash lock owned by the trigger.', 1;
    END

    IF @IsReleased = 0
    BEGIN
        UPDATE dbo.CashBalance
        SET Settled = Settled - @Amount,
            Reserved = Reserved - @Amount,
            DateModified = @Now,
            ChangedBy = @ChangedBy
        WHERE AccountId = @AccountId AND Currency = @Currency;

        UPDATE dbo.CashReservation
        SET IsReleased = 1,
            ReleasedUtc = @Now,
            DateModified = @Now,
            ChangedBy = @ChangedBy
        WHERE IdempotencyKey = @IdempotencyKey;

        COMMIT TRAN;

        SELECT b.*, CAST(0 AS BIT) AS AlreadyApplied
        FROM dbo.CashBalance b
        WHERE b.AccountId = @AccountId AND b.Currency = @Currency;
        SELECT * FROM dbo.CashReservation WHERE IdempotencyKey = @IdempotencyKey;
        RETURN;
    END

    COMMIT TRAN;

    SELECT b.*, CAST(1 AS BIT) AS AlreadyApplied
    FROM dbo.CashBalance b
    WHERE b.AccountId = @AccountId AND b.Currency = @Currency;
    SELECT * FROM dbo.CashReservation WHERE IdempotencyKey = @IdempotencyKey;
END
GO
