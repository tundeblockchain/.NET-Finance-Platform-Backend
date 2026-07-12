/*
  Procedure : dbo.LedgerEntry_u
  Purpose   : Upserts a LedgerEntry. On update, archives the current row into LedgerEntry_a before applying changes. Sets DateModified and ChangedBy.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.LedgerEntry_u
    @Id UNIQUEIDENTIFIER,
    @AccountId UNIQUEIDENTIFIER,
    @TriggerId UNIQUEIDENTIFIER,
    @AllocationRequestId UNIQUEIDENTIFIER,
    @EntryType INT,
    @Amount DECIMAL(18, 4),
    @Currency NVARCHAR(3),
    @IdempotencyKey NVARCHAR(200),
    @Description NVARCHAR(500),
    @PostedUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.LedgerEntry WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.LedgerEntry_a (Id, AccountId, TriggerId, AllocationRequestId, EntryType, Amount, Currency, IdempotencyKey, Description, PostedUtc, DateModified, ChangedBy)
        SELECT Id, AccountId, TriggerId, AllocationRequestId, EntryType, Amount, Currency, IdempotencyKey, Description, PostedUtc, DateModified, ChangedBy
        FROM dbo.LedgerEntry WHERE Id = @Id;

        UPDATE dbo.LedgerEntry
        SET AccountId = @AccountId, TriggerId = @TriggerId, AllocationRequestId = @AllocationRequestId, EntryType = @EntryType,
            Amount = @Amount, Currency = @Currency, IdempotencyKey = @IdempotencyKey, Description = @Description,
            PostedUtc = @PostedUtc, DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.LedgerEntry (Id, AccountId, TriggerId, AllocationRequestId, EntryType, Amount, Currency, IdempotencyKey, Description, PostedUtc, DateModified, ChangedBy)
        VALUES (@Id, @AccountId, @TriggerId, @AllocationRequestId, @EntryType, @Amount, @Currency, @IdempotencyKey, @Description, @PostedUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.LedgerEntry WHERE Id = @Id;
END
GO
