-- ============================================================================
-- TEST SOURCE CONFIGURATION — METADATA SETUP
-- ============================================================================
-- Object: Configure TestSourceDB.dbo.Customers in _meta schema
-- Date: 2026-05-16
-- Author: db-developer
-- Description: Inserts test configuration into _meta.Sources, _meta.SourceTables, _meta.SourceFields
--              Landing schema: [test], Landing table: [Customers]
--              Business key: CustomerId
-- ============================================================================

USE [DwBuilderDW];
GO

SET NOCOUNT ON;

-- ============================================================================
-- 1. INSERT SOURCE DEFINITION
-- ============================================================================

DECLARE @SourceId INT;

-- Check if test source already exists
IF EXISTS (SELECT 1 FROM _meta.Sources WHERE [Name] = N'TestSource')
BEGIN
    SELECT @SourceId = Id FROM _meta.Sources WHERE [Name] = N'TestSource';
    PRINT 'Test source already exists (Id: ' + CAST(@SourceId AS NVARCHAR(10)) + '). Skipping insert.';
END
ELSE
BEGIN
    INSERT INTO _meta.Sources (
        [Name],
        ServerName,
        InstanceName,
        DatabaseName,
        LandingSchema,
        ConnectionUser,
        ConnectionPasswordEncrypted,
        IsActive,
        CreatedAt,
        UpdatedAt
    )
    VALUES (
        N'TestSource',
        N'dw-sqlserver',        -- Same SQL Server container
        NULL,                   -- Default instance
        N'TestSourceDB',
        N'test',                -- Landing schema in DW
        N'sa',
        NULL,                   -- Password managed separately (not in clear text)
        1,                      -- Active
        GETUTCDATE(),
        GETUTCDATE()
    );

    SET @SourceId = SCOPE_IDENTITY();
    PRINT 'Test source created (Id: ' + CAST(@SourceId AS NVARCHAR(10)) + ').';
END
GO

-- ============================================================================
-- 2. INSERT SOURCE TABLE DEFINITION
-- ============================================================================

DECLARE @SourceId INT;
DECLARE @SourceTableId INT;

SELECT @SourceId = Id FROM _meta.Sources WHERE [Name] = N'TestSource';

IF EXISTS (
    SELECT 1 
    FROM _meta.SourceTables 
    WHERE SourceId = @SourceId 
      AND SchemaName = N'dbo' 
      AND TableName = N'Customers'
)
BEGIN
    SELECT @SourceTableId = Id 
    FROM _meta.SourceTables 
    WHERE SourceId = @SourceId 
      AND SchemaName = N'dbo' 
      AND TableName = N'Customers';
    
    PRINT 'Table Customers already configured (Id: ' + CAST(@SourceTableId AS NVARCHAR(10)) + '). Skipping insert.';
END
ELSE
BEGIN
    INSERT INTO _meta.SourceTables (
        SourceId,
        SchemaName,
        TableName,
        LandingTableName,
        IsActive,
        LastSyncAt,
        LastSyncStatus,
        LastSyncMessage,
        CreatedAt,
        UpdatedAt
    )
    VALUES (
        @SourceId,
        N'dbo',
        N'Customers',
        N'Customers',           -- Same name in landing
        1,                      -- Active
        NULL,                   -- Never synced yet
        NULL,
        NULL,
        GETUTCDATE(),
        GETUTCDATE()
    );

    SET @SourceTableId = SCOPE_IDENTITY();
    PRINT 'Table Customers configured (Id: ' + CAST(@SourceTableId AS NVARCHAR(10)) + ').';
END
GO

-- ============================================================================
-- 3. INSERT SOURCE FIELDS CONFIGURATION
-- ============================================================================

DECLARE @SourceTableId INT;

SELECT @SourceTableId = Id 
FROM _meta.SourceTables st
JOIN _meta.Sources s ON st.SourceId = s.Id
WHERE s.[Name] = N'TestSource' 
  AND st.SchemaName = N'dbo' 
  AND st.TableName = N'Customers';

-- Delete existing fields to avoid duplicates (idempotent script)
DELETE FROM _meta.SourceFields WHERE SourceTableId = @SourceTableId;

INSERT INTO _meta.SourceFields (
    SourceTableId,
    SourceColumnName,
    LandingColumnName,
    SqlDataType,
    IsBusinessKey,
    IsNullable,
    OrdinalPosition,
    CreatedAt,
    UpdatedAt
)
VALUES
    -- Business Key
    (@SourceTableId, N'CustomerId',     N'CustomerId',      N'INT',             1, 0, 1, GETUTCDATE(), GETUTCDATE()),
    
    -- Non-Key Fields
    (@SourceTableId, N'CustomerCode',   N'CustomerCode',    N'NVARCHAR(20)',    0, 0, 2, GETUTCDATE(), GETUTCDATE()),
    (@SourceTableId, N'CustomerName',   N'CustomerName',    N'NVARCHAR(100)',   0, 0, 3, GETUTCDATE(), GETUTCDATE()),
    (@SourceTableId, N'Email',          N'Email',           N'NVARCHAR(100)',   0, 1, 4, GETUTCDATE(), GETUTCDATE()),
    (@SourceTableId, N'City',           N'City',            N'NVARCHAR(50)',    0, 1, 5, GETUTCDATE(), GETUTCDATE()),
    (@SourceTableId, N'IsActive',       N'IsActive',        N'BIT',             0, 0, 6, GETUTCDATE(), GETUTCDATE());

PRINT 'Source fields configured (6 fields).';
GO

-- ============================================================================
-- 4. VALIDATION QUERY
-- ============================================================================

SELECT 
    s.[Name] AS SourceName,
    st.SchemaName + '.' + st.TableName AS SourceTable,
    st.LandingTableName,
    COUNT(sf.Id) AS ConfiguredFields
FROM _meta.Sources s
JOIN _meta.SourceTables st ON s.Id = st.SourceId
LEFT JOIN _meta.SourceFields sf ON st.Id = sf.SourceTableId
WHERE s.[Name] = N'TestSource'
GROUP BY s.[Name], st.SchemaName, st.TableName, st.LandingTableName;
GO

-- ============================================================================
-- SCRIPT COMPLETED
-- ============================================================================

PRINT 'Test source metadata configuration completed successfully.';
PRINT 'Next step: Execute 04_CreateLandingAndStagingTables.sql';
GO
