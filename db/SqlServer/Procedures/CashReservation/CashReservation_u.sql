/*
  Procedure : dbo.CashReservation_u
  Purpose   : Upserts a CashReservation. On update, archives the current row into CashReservation_a before applying changes. Sets DateModified and ChangedBy.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.CashReservation_u
    @Id UNIQUEIDENTIFIER,
    @AccountId UNIQUEIDENTIFIER,
    @AllocationRequestId UNIQUEIDENTIFIER,
    @TriggerId UNIQUEIDENTIFIER,
    @Currency NVARCHAR(3),
    @Amount DECIMAL(18, 4),
    @IdempotencyKey NVARCHAR(200),
    @IsReleased BIT,
    @CreatedUtc DATETIMEOFFSET,
    @ReleasedUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.CashReservation WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.CashReservation_a (Id, AccountId, AllocationRequestId, TriggerId, Currency, Amount, IdempotencyKey, IsReleased, CreatedUtc, ReleasedUtc, DateModified, ChangedBy)
        SELECT Id, AccountId, AllocationRequestId, TriggerId, Currency, Amount, IdempotencyKey, IsReleased, CreatedUtc, ReleasedUtc, DateModified, ChangedBy
        FROM dbo.CashReservation WHERE Id = @Id;

        UPDATE dbo.CashReservation
        SET AccountId = @AccountId, AllocationRequestId = @AllocationRequestId, TriggerId = @TriggerId, Currency = @Currency,
            Amount = @Amount, IdempotencyKey = @IdempotencyKey, IsReleased = @IsReleased, CreatedUtc = @CreatedUtc,
            ReleasedUtc = @ReleasedUtc, DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.CashReservation (Id, AccountId, AllocationRequestId, TriggerId, Currency, Amount, IdempotencyKey, IsReleased, CreatedUtc, ReleasedUtc, DateModified, ChangedBy)
        VALUES (@Id, @AccountId, @AllocationRequestId, @TriggerId, @Currency, @Amount, @IdempotencyKey, @IsReleased, @CreatedUtc, @ReleasedUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.CashReservation WHERE Id = @Id;
END
GO
