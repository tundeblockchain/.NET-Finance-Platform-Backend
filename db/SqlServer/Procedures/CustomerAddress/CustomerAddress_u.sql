/*
  Procedure : dbo.CustomerAddress_u
  Purpose   : Upserts a CustomerAddress. On update, archives into CustomerAddress_a. Sets DateModified and ChangedBy.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.CustomerAddress_u
    @Id UNIQUEIDENTIFIER,
    @CustomerId INT,
    @Line1 NVARCHAR(200),
    @Line2 NVARCHAR(200) = NULL,
    @City NVARCHAR(100),
    @Region NVARCHAR(100) = NULL,
    @PostalCode NVARCHAR(32),
    @Country NVARCHAR(100),
    @ChangedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.CustomerAddress WHERE Id = @Id)
    BEGIN
        INSERT INTO dbo.CustomerAddress_a (Id, CustomerId, Line1, Line2, City, Region, PostalCode, Country, DateModified, ChangedBy)
        SELECT Id, CustomerId, Line1, Line2, City, Region, PostalCode, Country, DateModified, ChangedBy
        FROM dbo.CustomerAddress WHERE Id = @Id;

        UPDATE dbo.CustomerAddress
        SET CustomerId = @CustomerId, Line1 = @Line1, Line2 = @Line2, City = @City, Region = @Region,
            PostalCode = @PostalCode, Country = @Country, DateModified = SYSUTCDATETIME(), ChangedBy = @ChangedBy
        WHERE Id = @Id;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.CustomerAddress (Id, CustomerId, Line1, Line2, City, Region, PostalCode, Country, DateModified, ChangedBy)
        VALUES (@Id, @CustomerId, @Line1, @Line2, @City, @Region, @PostalCode, @Country, SYSUTCDATETIME(), @ChangedBy);
    END

    SELECT * FROM dbo.CustomerAddress WHERE Id = @Id;
END
GO
