-- ============================================================================
-- SSIS VALIDATION — TEST #38: First Load (INSERT)
-- ============================================================================
-- Object: Validate initial data load into [test].[Customers]
-- Date: 2026-05-16
-- Author: db-developer
-- Description: Validates that SSIS package correctly inserts all records
--              from TestSourceDB.dbo.Customers into landing table
-- ============================================================================
-- PREREQUISITE: Execute SSIS package ETL_test_Customers.dtsx once
-- ============================================================================

USE [DwBuilderDW];
GO

SET NOCOUNT ON;

PRINT '============================================================================';
PRINT 'SSIS VALIDATION — TEST #38: First Load (INSERT)';
PRINT '============================================================================';
PRINT '';

-- ============================================================================
-- 1. VERIFY RECORD COUNT
-- ============================================================================

DECLARE @SourceCount INT;
DECLARE @LandingCount INT;

SELECT @SourceCount = COUNT(*) FROM [TestSourceDB].[dbo].[Customers];
SELECT @LandingCount = COUNT(*) FROM [test].[Customers];

PRINT '1. RECORD COUNT VALIDATION';
PRINT '   Source table: ' + CAST(@SourceCount AS NVARCHAR(10)) + ' records';
PRINT '   Landing table: ' + CAST(@LandingCount AS NVARCHAR(10)) + ' records';

IF @SourceCount = @LandingCount
    PRINT '   ✅ PASS: Record counts match';
ELSE
    PRINT '   ❌ FAIL: Record count mismatch!';

PRINT '';

-- ============================================================================
-- 2. VERIFY CHANGEHASHKEY POPULATED
-- ============================================================================

DECLARE @NullHashCount INT;

SELECT @NullHashCount = COUNT(*) 
FROM [test].[Customers] 
WHERE [ChangeHashKey] IS NULL OR [ChangeHashKey] = '';

PRINT '2. CHANGEHASHKEY VALIDATION';
PRINT '   Records with NULL or empty hash: ' + CAST(@NullHashCount AS NVARCHAR(10));

IF @NullHashCount = 0
    PRINT '   ✅ PASS: All records have valid ChangeHashKey';
ELSE
    PRINT '   ❌ FAIL: Some records have NULL or empty ChangeHashKey!';

PRINT '';

-- ============================================================================
-- 3. VERIFY INSERTDATETIME POPULATED
-- ============================================================================

DECLARE @NullInsertDateCount INT;

SELECT @NullInsertDateCount = COUNT(*) 
FROM [test].[Customers] 
WHERE [InsertDatetime] IS NULL;

PRINT '3. INSERTDATETIME VALIDATION';
PRINT '   Records with NULL InsertDatetime: ' + CAST(@NullInsertDateCount AS NVARCHAR(10));

IF @NullInsertDateCount = 0
    PRINT '   ✅ PASS: All records have InsertDatetime populated';
ELSE
    PRINT '   ❌ FAIL: Some records have NULL InsertDatetime!';

PRINT '';

-- ============================================================================
-- 4. VERIFY ISDELETED = 0 (No soft-deletes on first load)
-- ============================================================================

DECLARE @DeletedCount INT;

SELECT @DeletedCount = COUNT(*) 
FROM [test].[Customers] 
WHERE [IsDeleted] = 1;

PRINT '4. SOFT-DELETE VALIDATION';
PRINT '   Records marked as deleted: ' + CAST(@DeletedCount AS NVARCHAR(10));

IF @DeletedCount = 0
    PRINT '   ✅ PASS: No records marked as deleted';
ELSE
    PRINT '   ❌ FAIL: Some records incorrectly marked as deleted!';

PRINT '';

-- ============================================================================
-- 5. VERIFY UPDATEDATETIME = INSERTDATETIME (First load, no updates yet)
-- ============================================================================

DECLARE @MismatchDateCount INT;

SELECT @MismatchDateCount = COUNT(*) 
FROM [test].[Customers] 
WHERE [UpdateDatetime] <> [InsertDatetime];

PRINT '5. DATETIME CONSISTENCY VALIDATION';
PRINT '   Records with UpdateDatetime != InsertDatetime: ' + CAST(@MismatchDateCount AS NVARCHAR(10));

IF @MismatchDateCount = 0
    PRINT '   ✅ PASS: All records have matching Insert/Update datetime';
ELSE
    PRINT '   ⚠️  WARNING: Some records have different Insert/Update datetime (expected on first load)';

PRINT '';

-- ============================================================================
-- 6. SAMPLE DATA OUTPUT (First 5 records)
-- ============================================================================

PRINT '6. SAMPLE DATA (First 5 records with hash)';
PRINT '';

SELECT TOP 5
    [CustomerId],
    [CustomerCode],
    [CustomerName],
    LEFT([ChangeHashKey], 16) + '...' AS HashPreview,
    [InsertDatetime],
    [UpdateDatetime],
    [IsDeleted],
    [IsActive]
FROM [test].[Customers]
ORDER BY [CustomerId];

PRINT '';
PRINT '============================================================================';
PRINT 'VALIDATION COMPLETED';
PRINT '============================================================================';
PRINT '';
PRINT 'Next test: 02_ValidateUpdate.sql';
GO
