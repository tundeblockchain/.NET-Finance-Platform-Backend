/*
  Procedure : dbo.get_InvestmentInstruction_ByIdempotencyKey_f
  Purpose   : Fetches an InvestmentInstruction by IdempotencyKey.
  Dated     : 2026-07-13
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_InvestmentInstruction_ByIdempotencyKey_f
    @IdempotencyKey NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.InvestmentInstruction WHERE IdempotencyKey = @IdempotencyKey;
END
GO
