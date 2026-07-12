/*
  Procedure : dbo.Customer_u
  Purpose   : Upserts a Customer. On update, archives into Customer_a. Insert when @Id is NULL or 0 (IDENTITY). Sets DateModified and ChangedBy.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.Customer_u
    @Id INT = NULL OUTPUT,
    @Email NVARCHAR(256),
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @CreatedUtc DATETIMEOFFSET,
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NOT NULL AND @Id > 0 AND EXISTS (SELECT 1 FROM dbo.Customer WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.Customer_a (Id, Email, FirstName, LastName, CreatedUtc, DateModified, ChangedBy)
        SELECT Id, Email, FirstName, LastName, CreatedUtc, DateModified, ChangedBy
        FROM dbo.Customer WHERE Id = @Id;

        UPDATE dbo.Customer
        SET Email = @Email, FirstName = @FirstName, LastName = @LastName,
            CreatedUtc = @CreatedUtc, DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Customer (Email, FirstName, LastName, CreatedUtc, DateModified, ChangedBy)
        VALUES (@Email, @FirstName, @LastName, @CreatedUtc, SYSUTCDATETIME(), @ChangedBy);

        SET @Id = CAST(SCOPE_IDENTITY() AS INT);
    END

    SELECT * FROM dbo.Customer WHERE Id = @Id;
END
GO
