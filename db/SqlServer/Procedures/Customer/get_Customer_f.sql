/*
  Procedure : dbo.get_Customer_f
  Purpose   : Fetches a Customer by Id.
  Dated     : 2026-07-12
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.get_Customer_f
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Customer WHERE Id = @Id;
END
GO
