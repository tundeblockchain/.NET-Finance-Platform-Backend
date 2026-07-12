/*
  Procedure : dbo.CreateLedgerEntry
  Purpose   : Creates a ledger posting idempotently by IdempotencyKey. Returns the existing entry when the key was already applied.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.CreateLedgerEntry
    @Id UNIQUEIDENTIFIER,
    @AccountId UNIQUEIDENTIFIER,
    @TriggerId UNIQUEIDENTIFIER = NULL,
    @AllocationRequestId UNIQUEIDENTIFIER = NULL,
    @EntryType INT,
    @Amount DECIMAL(18, 4),
    @Currency NVARCHAR(3),
    @IdempotencyKey NVARCHAR(200),
    @Description NVARCHAR(500),
    @ChangedBy NVARCHAR(100) = N'broker'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();

    IF EXISTS (SELECT 1 FROM dbo.LedgerEntry WHERE IdempotencyKey = @IdempotencyKey)
    BEGIN
        SELECT *, CAST(1 AS BIT) AS AlreadyApplied
        FROM dbo.LedgerEntry
        WHERE IdempotencyKey = @IdempotencyKey;
        RETURN;
    END

    INSERT INTO dbo.LedgerEntry
    (
        Id, AccountId, TriggerId, AllocationRequestId, EntryType, Amount, Currency,
        IdempotencyKey, Description, PostedUtc, DateModified, ChangedBy
    )
    VALUES
    (
        @Id, @AccountId, @TriggerId, @AllocationRequestId, @EntryType, @Amount, @Currency,
        @IdempotencyKey, @Description, @Now, @Now, @ChangedBy
    );

    SELECT *, CAST(0 AS BIT) AS AlreadyApplied
    FROM dbo.LedgerEntry
    WHERE Id = @Id;
END
GO
