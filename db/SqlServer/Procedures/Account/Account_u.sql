/*
  Procedure : dbo.Account_u
  Purpose   : Upserts an Account. On update, archives the current row into Account_a before applying changes. Sets DateModified and ChangedBy.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.Account_u
    @Id UNIQUEIDENTIFIER,
    @CustomerId UNIQUEIDENTIFIER,
    @AccountNumber NVARCHAR(64),
    @Currency NVARCHAR(3),
    @IsActive BIT,
    @CreatedUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Account WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.Account_a (Id, CustomerId, AccountNumber, Currency, IsActive, CreatedUtc, DateModified, ChangedBy)
        SELECT Id, CustomerId, AccountNumber, Currency, IsActive, CreatedUtc, DateModified, ChangedBy
        FROM dbo.Account WHERE Id = @Id;

        UPDATE dbo.Account
        SET CustomerId = @CustomerId, AccountNumber = @AccountNumber, Currency = @Currency,
            IsActive = @IsActive, CreatedUtc = @CreatedUtc, DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Account (Id, CustomerId, AccountNumber, Currency, IsActive, CreatedUtc, DateModified, ChangedBy)
        VALUES (@Id, @CustomerId, @AccountNumber, @Currency, @IsActive, @CreatedUtc, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.Account WHERE Id = @Id;
END
GO
