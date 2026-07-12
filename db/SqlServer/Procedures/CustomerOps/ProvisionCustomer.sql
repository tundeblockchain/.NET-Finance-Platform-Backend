/*
  Procedure : dbo.ProvisionCustomer
  Purpose   : Atomically creates Customer, optional address, CustomerAccount, TradingAccount, park distribution agreement and element (702).
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.ProvisionCustomer
    @Email NVARCHAR(256),
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @Currency NVARCHAR(3),
    @Line1 NVARCHAR(200) = NULL,
    @Line2 NVARCHAR(200) = NULL,
    @City NVARCHAR(100) = NULL,
    @Region NVARCHAR(100) = NULL,
    @PostalCode NVARCHAR(32) = NULL,
    @Country NVARCHAR(100) = NULL,
    @ChangedBy NVARCHAR(100) = N'system'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();
    DECLARE @CustomerId INT;
    DECLARE @CustomerAccountId UNIQUEIDENTIFIER = NEWID();
    DECLARE @TradingAccountId UNIQUEIDENTIFIER = NEWID();
    DECLARE @AgreementId UNIQUEIDENTIFIER = NEWID();
    DECLARE @ElementId UNIQUEIDENTIFIER = NEWID();
    DECLARE @AddressId UNIQUEIDENTIFIER = NULL;

    BEGIN TRAN;

    INSERT INTO dbo.Customer (Email, FirstName, LastName, CreatedUtc, DateModified, ChangedBy)
    VALUES (@Email, @FirstName, @LastName, @Now, @Now, @ChangedBy);

    SET @CustomerId = CAST(SCOPE_IDENTITY() AS INT);

    IF @Line1 IS NOT NULL
    BEGIN
        SET @AddressId = NEWID();
        INSERT INTO dbo.CustomerAddress (Id, CustomerId, Line1, Line2, City, Region, PostalCode, Country, DateModified, ChangedBy)
        VALUES (@AddressId, @CustomerId, @Line1, @Line2, @City, @Region, @PostalCode, @Country, @Now, @ChangedBy);
    END

    INSERT INTO dbo.CustomerAccount (Id, CustomerId, Currency, Settled, Reserved, IsLocked, LockedByTriggerId, LockExpiresUtc, CreatedUtc, DateModified, ChangedBy)
    VALUES (@CustomerAccountId, @CustomerId, @Currency, 0, 0, 0, NULL, NULL, @Now, @Now, @ChangedBy);

    INSERT INTO dbo.TradingAccount (Id, CustomerId, Currency, Settled, Reserved, IsLocked, LockedByTriggerId, LockExpiresUtc, CreatedUtc, DateModified, ChangedBy)
    VALUES (@TradingAccountId, @CustomerId, @Currency, 0, 0, 0, NULL, NULL, @Now, @Now, @ChangedBy);

    INSERT INTO dbo.DistributionAgreement (Id, CustomerId, OwnerComponent, OwnerAccountId, Name, IsActive, CreatedUtc, DateModified, ChangedBy)
    VALUES (@AgreementId, @CustomerId, 1 /* Customer */, @CustomerAccountId, N'Customer → Trading (park)', 1, @Now, @Now, @ChangedBy);

    INSERT INTO dbo.DistributionElement (Id, AgreementId, TargetType, TargetAccountId, Percentage, Priority, DateModified, ChangedBy)
    VALUES (@ElementId, @AgreementId, 702 /* TradingAccount */, @TradingAccountId, 1, 1, @Now, @ChangedBy);

    COMMIT TRAN;

    SELECT * FROM dbo.Customer WHERE Id = @CustomerId;
    SELECT * FROM dbo.CustomerAddress WHERE CustomerId = @CustomerId;
    SELECT * FROM dbo.CustomerAccount WHERE Id = @CustomerAccountId;
    SELECT * FROM dbo.TradingAccount WHERE Id = @TradingAccountId;
    SELECT * FROM dbo.DistributionAgreement WHERE Id = @AgreementId;
END
GO
