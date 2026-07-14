/*
    Clears all user-table data in the current database.
    Disables FK checks, DELETEs every user table, reseeds identities, then re-enables FKs.
    Does not drop tables, procedures, or schema objects.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @sql NVARCHAR(MAX) = N'';

-- Disable all foreign-key constraints on user tables
SELECT @sql += N'ALTER TABLE '
    + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
    + N' NOCHECK CONSTRAINT ALL;' + CHAR(13)
FROM sys.tables AS t
WHERE t.is_ms_shipped = 0
  AND t.temporal_type IN (0, 2); -- exclude history tables (type 1)

EXEC sys.sp_executesql @sql;

-- Delete all rows
SET @sql = N'';
SELECT @sql += N'DELETE FROM '
    + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
    + N';' + CHAR(13)
FROM sys.tables AS t
WHERE t.is_ms_shipped = 0
  AND t.temporal_type IN (0, 2);

EXEC sys.sp_executesql @sql;

-- Reseed identity columns (no-op when none exist)
SET @sql = N'';
SELECT @sql += N'DBCC CHECKIDENT ('
    + QUOTENAME(SCHEMA_NAME(t.schema_id) + N'.' + t.name, '''')
    + N', RESEED, 0);' + CHAR(13)
FROM sys.tables AS t
INNER JOIN sys.identity_columns AS ic ON ic.object_id = t.object_id
WHERE t.is_ms_shipped = 0
  AND t.temporal_type IN (0, 2);

IF LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

-- Re-enable and validate foreign-key constraints
SET @sql = N'';
SELECT @sql += N'ALTER TABLE '
    + QUOTENAME(SCHEMA_NAME(t.schema_id)) + N'.' + QUOTENAME(t.name)
    + N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(13)
FROM sys.tables AS t
WHERE t.is_ms_shipped = 0
  AND t.temporal_type IN (0, 2);

EXEC sys.sp_executesql @sql;

COMMIT TRANSACTION;

PRINT N'All user tables cleared.';
GO
