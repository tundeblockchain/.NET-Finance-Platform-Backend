/*
  Procedure : dbo.get_CustomerAddress_f
  Purpose   : Fetches a CustomerAddress by Id.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_CustomerAddress_f
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.CustomerAddress WHERE Id = @Id;
END
GO
