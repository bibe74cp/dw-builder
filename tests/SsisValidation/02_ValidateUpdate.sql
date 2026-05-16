-- ============================================================================
-- SSIS VALIDATION — TEST #39: Change Detection (UPDATE)
-- ============================================================================
-- Object: Validate SSIS package detects changes and updates ChangeHashKey
-- Date: 2026-05-16
-- Author: db-developer
-- Description: Part 1 modifies source data, Part 2 validates update detection
-- ============================================================================
-- EXECUTION FLOW:
-- 1. Run Part 1 (modify source data)
-- 2. Execute SSIS package ETL_test_Customers.dtsx
-- 3. Run Part 2 (validate results)
-- ============================================================================

USE [DwBuilderDW];
GO

SET NOCOUNT ON;

-- ============================================================================
-- PART 1: MODIFY SOURCE DATA
-- ============================================================================
-- Run this BEFORE executing SSIS package

PRINT '============================================================================';
PRINT 'TEST #39 — PART 1: Modifying source data';
PRINT '============================================================================';
PRINT '';

-- Capture current state before modification
IF OBJECT_ID('tempdb..#PreUpdateSnapshot') IS NOT NULL
    DROP TABLE #PreUpdateSnapshot;

SELECT 
    [CustomerId],
    [ChangeHashKey] AS OldHash,
    [UpdateDatetime] AS OldUpdateDatetime,
    [Email] AS OldEmail
INTO #PreUpdateSnapshot
FROM [test].[Customers]
WHERE [CustomerId] IN (1, 5, 10);

PRINT 'Snapshot created for CustomerId: 1, 5, 10';
PRINT '';

-- Modify 3 customers in source database
PRINT 'Modifying 3 customers in TestSourceDB...';

EXEC [TestSourceDB]..sp_executesql N'
    UPDATE dbo.Customers 
    SET Email = ''updated001@testmail.com''
    WHERE CustomerId = 1;

    UPDATE dbo.Customers 
    SET Email = ''updated005@testmail.com'', City = ''Updated City 5''
    WHERE CustomerId = 5;

    UPDATE dbo.Customers 
    SET CustomerName = ''Updated Customer 010''
    WHERE CustomerId = 10;
';

PRINT '✅ Source data modified:';
PRINT '   - CustomerId 1: Email changed';
PRINT '   - CustomerId 5: Email and City changed';
PRINT '   - CustomerId 10: CustomerName changed';
PRINT '';
PRINT '⚠️  NEXT STEP: Execute SSIS package ETL_test_Customers.dtsx';
PRINT '⚠️  THEN: Run Part 2 of this script';
PRINT '';
PRINT '============================================================================';
GO

-- ============================================================================
-- PART 2: VALIDATE UPDATE DETECTION
-- ============================================================================
-- Run this AFTER executing SSIS package

/*
PRINT '============================================================================';
PRINT 'TEST #39 — PART 2: Validating update detection';
PRINT '============================================================================';
PRINT '';

-- ============================================================================
-- 1. VERIFY CHANGEHASHKEY CHANGED FOR MODIFIED RECORDS
-- ============================================================================

PRINT '1. CHANGEHASHKEY CHANGE VALIDATION';
PRINT '';

SELECT 
    curr.[CustomerId],
    pre.OldHash AS PreviousHash,
    curr.[ChangeHashKey] AS CurrentHash,
    CASE 
        WHEN pre.OldHash <> curr.[ChangeHashKey] THEN '✅ CHANGED'
        ELSE '❌ UNCHANGED'
    END AS ValidationStatus
FROM [test].[Customers] curr
JOIN #PreUpdateSnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
ORDER BY curr.[CustomerId];

DECLARE @ChangedHashCount INT;

SELECT @ChangedHashCount = COUNT(*)
FROM [test].[Customers] curr
JOIN #PreUpdateSnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
WHERE pre.OldHash <> curr.[ChangeHashKey];

PRINT '';
PRINT '   Modified records with changed hash: ' + CAST(@ChangedHashCount AS NVARCHAR(10)) + ' / 3';

IF @ChangedHashCount = 3
    PRINT '   ✅ PASS: All modified records have new ChangeHashKey';
ELSE
    PRINT '   ❌ FAIL: Some modified records have unchanged ChangeHashKey!';

PRINT '';

-- ============================================================================
-- 2. VERIFY UPDATEDATETIME UPDATED
-- ============================================================================

PRINT '2. UPDATEDATETIME VALIDATION';
PRINT '';

SELECT 
    curr.[CustomerId],
    pre.OldUpdateDatetime AS PreviousUpdateDatetime,
    curr.[UpdateDatetime] AS CurrentUpdateDatetime,
    DATEDIFF(SECOND, pre.OldUpdateDatetime, curr.[UpdateDatetime]) AS SecondsDifference,
    CASE 
        WHEN curr.[UpdateDatetime] > pre.OldUpdateDatetime THEN '✅ UPDATED'
        ELSE '❌ NOT UPDATED'
    END AS ValidationStatus
FROM [test].[Customers] curr
JOIN #PreUpdateSnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
ORDER BY curr.[CustomerId];

DECLARE @UpdatedDateCount INT;

SELECT @UpdatedDateCount = COUNT(*)
FROM [test].[Customers] curr
JOIN #PreUpdateSnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
WHERE curr.[UpdateDatetime] > pre.OldUpdateDatetime;

PRINT '';
PRINT '   Modified records with updated UpdateDatetime: ' + CAST(@UpdatedDateCount AS NVARCHAR(10)) + ' / 3';

IF @UpdatedDateCount = 3
    PRINT '   ✅ PASS: All modified records have new UpdateDatetime';
ELSE
    PRINT '   ❌ FAIL: Some modified records have unchanged UpdateDatetime!';

PRINT '';

-- ============================================================================
-- 3. VERIFY UNMODIFIED RECORDS UNCHANGED
-- ============================================================================

PRINT '3. UNMODIFIED RECORDS VALIDATION';
PRINT '';

DECLARE @UnchangedRecordsWithChangedHash INT;

SELECT @UnchangedRecordsWithChangedHash = COUNT(*)
FROM [test].[Customers] curr
LEFT JOIN #PreUpdateSnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
WHERE pre.[CustomerId] IS NULL -- Not in modified set
  AND curr.[CustomerId] BETWEEN 1 AND 20 -- Test data range
  AND NOT EXISTS (
      SELECT 1 FROM [test].[Customers] base
      WHERE base.[CustomerId] = curr.[CustomerId]
        AND base.[InsertDatetime] = base.[UpdateDatetime]
  );

PRINT '   Unmodified records with changed hash: ' + CAST(@UnchangedRecordsWithChangedHash AS NVARCHAR(10));

IF @UnchangedRecordsWithChangedHash = 0
    PRINT '   ✅ PASS: Unmodified records have unchanged hash';
ELSE
    PRINT '   ⚠️  WARNING: Some unmodified records have changed hash';

PRINT '';

-- ============================================================================
-- 4. VERIFY EMAIL VALUES UPDATED
-- ============================================================================

PRINT '4. BUSINESS DATA VALIDATION';
PRINT '';

SELECT 
    curr.[CustomerId],
    pre.OldEmail AS PreviousEmail,
    curr.[Email] AS CurrentEmail,
    CASE 
        WHEN pre.OldEmail <> curr.[Email] THEN '✅ UPDATED'
        ELSE '❌ UNCHANGED'
    END AS ValidationStatus
FROM [test].[Customers] curr
JOIN #PreUpdateSnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
ORDER BY curr.[CustomerId];

PRINT '';
PRINT '============================================================================';
PRINT 'VALIDATION COMPLETED';
PRINT '============================================================================';
PRINT '';
PRINT 'Next test: 03_ValidateSoftDelete.sql';

-- Cleanup
DROP TABLE #PreUpdateSnapshot;

GO
*/
