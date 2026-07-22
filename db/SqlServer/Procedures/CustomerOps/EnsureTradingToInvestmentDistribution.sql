/*
  Procedure : dbo.EnsureTradingToInvestmentDistribution
  Purpose   : Creates Trading → Investment distribution agreement + element if missing.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.EnsureTradingToInvestmentDistribution
    @CustomerId INT,
    @TradingAccountId UNIQUEIDENTIFIER,
    @InvestmentAccountId UNIQUEIDENTIFIER,
    @ChangedBy NVARCHAR(100) = N'system'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE @AgreementId UNIQUEIDENTIFIER;
    SELECT TOP (1) @AgreementId = Id
    FROM dbo.DistributionAgreement
    WHERE OwnerAccountId = @TradingAccountId AND IsActive = 1;

    IF @AgreementId IS NOT NULL
    BEGIN
        SELECT * FROM dbo.DistributionAgreement WHERE Id = @AgreementId;
        COMMIT TRAN;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.InvestmentAccount WHERE Id = @InvestmentAccountId AND TradingAccountId = @TradingAccountId)
    BEGIN
        ROLLBACK TRAN;
        THROW 50045, 'Investment account does not belong to trading account.', 1;
    END

    DECLARE @Now DATETIMEOFFSET = SYSUTCDATETIME();
    SET @AgreementId = NEWID();
    DECLARE @ElementId UNIQUEIDENTIFIER = NEWID();
    -- ComponentType.Trading = 2, DistributionTargetType.InvestmentAccount = 802
    DECLARE @OwnerComponent INT = 2;
    DECLARE @TargetType INT = 802;

    INSERT INTO dbo.DistributionAgreement (
        Id, CustomerId, OwnerComponent, OwnerAccountId, Name, IsActive, CreatedUtc, DateModified, ChangedBy)
    VALUES (
        @AgreementId, @CustomerId, @OwnerComponent, @TradingAccountId, N'Trading → Investment', 1, @Now, @Now, @ChangedBy);

    INSERT INTO dbo.DistributionElement (
        Id, AgreementId, TargetType, TargetAccountId, Percentage, Priority, DateModified, ChangedBy)
    VALUES (
        @ElementId, @AgreementId, @TargetType, @InvestmentAccountId, 1.0, 1, @Now, @ChangedBy);

    COMMIT TRAN;

    SELECT * FROM dbo.DistributionAgreement WHERE Id = @AgreementId;
END
GO
