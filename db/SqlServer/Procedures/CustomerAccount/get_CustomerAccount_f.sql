/*
  Procedure : dbo.get_CustomerAccount_f
  Purpose   : Fetches a CustomerAccount by Id.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_CustomerAccount_f
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.CustomerAccount WHERE Id = @Id;
END
GO
