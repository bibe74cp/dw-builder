-- ============================================================================
-- SSIS VALIDATION — TEST #40: Soft Delete Detection
-- ============================================================================
-- Object: Validate SSIS package marks deleted records with IsDeleted = 1
-- Date: 2026-05-16
-- Author: db-developer
-- Description: Part 1 deletes source records, Part 2 validates soft-delete logic
-- ============================================================================
-- EXECUTION FLOW:
-- 1. Run Part 1 (delete source records)
-- 2. Execute SSIS package ETL_test_Customers.dtsx
-- 3. Run Part 2 (validate soft-delete)
-- ============================================================================

USE [DwBuilderDW];
GO

SET NOCOUNT ON;

-- ============================================================================
-- PART 1: DELETE SOURCE RECORDS
-- ============================================================================
-- Run this BEFORE executing SSIS package

PRINT '============================================================================';
PRINT 'TEST #40 — PART 1: Deleting source records';
PRINT '============================================================================';
PRINT '';

-- Capture current state before deletion
IF OBJECT_ID('tempdb..#PreDeleteSnapshot') IS NOT NULL
    DROP TABLE #PreDeleteSnapshot;

SELECT 
    [CustomerId],
    [ChangeHashKey] AS OriginalHash,
    [UpdateDatetime] AS OriginalUpdateDatetime,
    [IsDeleted] AS OriginalIsDeleted
INTO #PreDeleteSnapshot
FROM [test].[Customers]
WHERE [CustomerId] IN (3, 7);

PRINT 'Snapshot created for CustomerId: 3, 7';
PRINT '';

-- Delete 2 customers from source database
PRINT 'Deleting 2 customers from TestSourceDB...';

EXEC [TestSourceDB]..sp_executesql N'
    DELETE FROM dbo.Customers WHERE CustomerId = 3;
    DELETE FROM dbo.Customers WHERE CustomerId = 7;
';

PRINT '✅ Source records deleted:';
PRINT '   - CustomerId 3';
PRINT '   - CustomerId 7';
PRINT '';
PRINT '⚠️  NEXT STEP: Execute SSIS package ETL_test_Customers.dtsx';
PRINT '⚠️  THEN: Run Part 2 of this script';
PRINT '';
PRINT '============================================================================';
GO

-- ============================================================================
-- PART 2: VALIDATE SOFT-DELETE
-- ============================================================================
-- Run this AFTER executing SSIS package

/*
PRINT '============================================================================';
PRINT 'TEST #40 — PART 2: Validating soft-delete detection';
PRINT '============================================================================';
PRINT '';

-- ============================================================================
-- 1. VERIFY ISDELETED = 1 FOR DELETED RECORDS
-- ============================================================================

PRINT '1. SOFT-DELETE FLAG VALIDATION';
PRINT '';

SELECT 
    curr.[CustomerId],
    pre.OriginalIsDeleted AS PreviousIsDeleted,
    curr.[IsDeleted] AS CurrentIsDeleted,
    CASE 
        WHEN curr.[IsDeleted] = 1 THEN '✅ MARKED DELETED'
        ELSE '❌ NOT MARKED'
    END AS ValidationStatus
FROM [test].[Customers] curr
JOIN #PreDeleteSnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
ORDER BY curr.[CustomerId];

DECLARE @DeletedCount INT;

SELECT @DeletedCount = COUNT(*)
FROM [test].[Customers] curr
JOIN #PreDeleteSnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
WHERE curr.[IsDeleted] = 1;

PRINT '';
PRINT '   Deleted records marked with IsDeleted = 1: ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' / 2';

IF @DeletedCount = 2
    PRINT '   ✅ PASS: All deleted records marked correctly';
ELSE
    PRINT '   ❌ FAIL: Some deleted records not marked!';

PRINT '';

-- ============================================================================
-- 2. VERIFY UPDATEDATETIME UPDATED ON SOFT-DELETE
-- ============================================================================

PRINT '2. UPDATEDATETIME VALIDATION (Soft-Delete)';
PRINT '';

SELECT 
    curr.[CustomerId],
    pre.OriginalUpdateDatetime AS PreviousUpdateDatetime,
    curr.[UpdateDatetime] AS CurrentUpdateDatetime,
    DATEDIFF(SECOND, pre.OriginalUpdateDatetime, curr.[UpdateDatetime]) AS SecondsDifference,
    CASE 
        WHEN curr.[UpdateDatetime] > pre.OriginalUpdateDatetime THEN '✅ UPDATED'
        ELSE '❌ NOT UPDATED'
    END AS ValidationStatus
FROM [test].[Customers] curr
JOIN #PreDeleteSnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
ORDER BY curr.[CustomerId];

DECLARE @UpdatedDateCount INT;

SELECT @UpdatedDateCount = COUNT(*)
FROM [test].[Customers] curr
JOIN #PreDeleteSnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
WHERE curr.[UpdateDatetime] > pre.OriginalUpdateDatetime;

PRINT '';
PRINT '   Deleted records with updated UpdateDatetime: ' + CAST(@UpdatedDateCount AS NVARCHAR(10)) + ' / 2';

IF @UpdatedDateCount = 2
    PRINT '   ✅ PASS: All deleted records have new UpdateDatetime';
ELSE
    PRINT '   ❌ FAIL: Some deleted records have unchanged UpdateDatetime!';

PRINT '';

-- ============================================================================
-- 3. VERIFY CHANGEHASHKEY UNCHANGED ON SOFT-DELETE
-- ============================================================================

PRINT '3. CHANGEHASHKEY VALIDATION (Should remain unchanged)';
PRINT '';

SELECT 
    curr.[CustomerId],
    pre.OriginalHash AS PreviousHash,
    curr.[ChangeHashKey] AS CurrentHash,
    CASE 
        WHEN pre.OriginalHash = curr.[ChangeHashKey] THEN '✅ UNCHANGED'
        ELSE '❌ CHANGED'
    END AS ValidationStatus
FROM [test].[Customers] curr
JOIN #PreDeleteSnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
ORDER BY curr.[CustomerId];

DECLARE @UnchangedHashCount INT;

SELECT @UnchangedHashCount = COUNT(*)
FROM [test].[Customers] curr
JOIN #PreDeleteSnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
WHERE pre.OriginalHash = curr.[ChangeHashKey];

PRINT '';
PRINT '   Deleted records with unchanged hash: ' + CAST(@UnchangedHashCount AS NVARCHAR(10)) + ' / 2';

IF @UnchangedHashCount = 2
    PRINT '   ✅ PASS: ChangeHashKey unchanged (correct behavior)';
ELSE
    PRINT '   ⚠️  WARNING: ChangeHashKey changed on soft-delete (unexpected)';

PRINT '';

-- ============================================================================
-- 4. VERIFY RECORDS STILL EXIST (Not hard-deleted)
-- ============================================================================

PRINT '4. RECORD EXISTENCE VALIDATION';
PRINT '';

DECLARE @ExistingRecords INT;

SELECT @ExistingRecords = COUNT(*)
FROM [test].[Customers]
WHERE [CustomerId] IN (3, 7);

PRINT '   Records still in landing table: ' + CAST(@ExistingRecords AS NVARCHAR(10)) + ' / 2';

IF @ExistingRecords = 2
    PRINT '   ✅ PASS: Records not hard-deleted (soft-delete only)';
ELSE
    PRINT '   ❌ FAIL: Records were hard-deleted from landing table!';

PRINT '';

-- ============================================================================
-- 5. VERIFY BUSINESS DATA UNCHANGED
-- ============================================================================

PRINT '5. BUSINESS DATA VALIDATION (Should remain unchanged)';
PRINT '';

SELECT 
    [CustomerId],
    [CustomerCode],
    [CustomerName],
    [Email],
    [IsDeleted],
    [UpdateDatetime]
FROM [test].[Customers]
WHERE [CustomerId] IN (3, 7)
ORDER BY [CustomerId];

PRINT '';
PRINT '============================================================================';
PRINT 'VALIDATION COMPLETED';
PRINT '============================================================================';
PRINT '';
PRINT 'Next test: 04_ValidateIdempotency.sql';

-- Cleanup
DROP TABLE #PreDeleteSnapshot;

GO
*/
