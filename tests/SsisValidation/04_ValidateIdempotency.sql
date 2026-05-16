-- ============================================================================
-- SSIS VALIDATION — TEST #41: Idempotency (Double Execution)
-- ============================================================================
-- Object: Validate SSIS package can run multiple times without duplicates
-- Date: 2026-05-16
-- Author: db-developer
-- Description: Verifies that executing the same SSIS package twice with
--              unchanged source data does NOT create duplicates or
--              modify records unnecessarily
-- ============================================================================
-- EXECUTION FLOW:
-- 1. Run Part 1 (capture state before double execution)
-- 2. Execute SSIS package ETL_test_Customers.dtsx TWICE consecutively
-- 3. Run Part 2 (validate no duplicates, no unnecessary updates)
-- ============================================================================

USE [DwBuilderDW];
GO

SET NOCOUNT ON;

-- ============================================================================
-- PART 1: CAPTURE STATE BEFORE DOUBLE EXECUTION
-- ============================================================================
-- Run this BEFORE executing SSIS package twice

PRINT '============================================================================';
PRINT 'TEST #41 — PART 1: Capturing state before double execution';
PRINT '============================================================================';
PRINT '';

-- Capture current state
IF OBJECT_ID('tempdb..#PreIdempotencySnapshot') IS NOT NULL
    DROP TABLE #PreIdempotencySnapshot;

SELECT 
    [CustomerId],
    [ChangeHashKey] AS OriginalHash,
    [UpdateDatetime] AS OriginalUpdateDatetime,
    [IsDeleted] AS OriginalIsDeleted
INTO #PreIdempotencySnapshot
FROM [test].[Customers];

DECLARE @RecordCountBefore INT;
SELECT @RecordCountBefore = COUNT(*) FROM [test].[Customers];

PRINT 'Snapshot created:';
PRINT '   - Total records: ' + CAST(@RecordCountBefore AS NVARCHAR(10));
PRINT '   - Snapshot table: #PreIdempotencySnapshot';
PRINT '';
PRINT '⚠️  NEXT STEP: Execute SSIS package ETL_test_Customers.dtsx TWICE';
PRINT '⚠️  THEN: Run Part 2 of this script';
PRINT '';
PRINT '============================================================================';
GO

-- ============================================================================
-- PART 2: VALIDATE IDEMPOTENCY
-- ============================================================================
-- Run this AFTER executing SSIS package TWICE

/*
PRINT '============================================================================';
PRINT 'TEST #41 — PART 2: Validating idempotency';
PRINT '============================================================================';
PRINT '';

-- ============================================================================
-- 1. VERIFY NO DUPLICATE RECORDS
-- ============================================================================

PRINT '1. DUPLICATE DETECTION VALIDATION';
PRINT '';

DECLARE @RecordCountAfter INT;
DECLARE @RecordCountBefore INT;

SELECT @RecordCountBefore = COUNT(*) FROM #PreIdempotencySnapshot;
SELECT @RecordCountAfter = COUNT(*) FROM [test].[Customers];

PRINT '   Records before double execution: ' + CAST(@RecordCountBefore AS NVARCHAR(10));
PRINT '   Records after double execution:  ' + CAST(@RecordCountAfter AS NVARCHAR(10));
PRINT '   Difference: ' + CAST((@RecordCountAfter - @RecordCountBefore) AS NVARCHAR(10));
PRINT '';

IF @RecordCountBefore = @RecordCountAfter
    PRINT '   ✅ PASS: No duplicate records created';
ELSE
    PRINT '   ❌ FAIL: Duplicate records detected!';

PRINT '';

-- Check for duplicate business keys
DECLARE @DuplicateKeyCount INT;

SELECT @DuplicateKeyCount = COUNT(*)
FROM (
    SELECT [CustomerId], COUNT(*) AS DuplicateCount
    FROM [test].[Customers]
    GROUP BY [CustomerId]
    HAVING COUNT(*) > 1
) AS Duplicates;

PRINT '   Business keys with duplicates: ' + CAST(@DuplicateKeyCount AS NVARCHAR(10));

IF @DuplicateKeyCount = 0
    PRINT '   ✅ PASS: No duplicate business keys';
ELSE
    PRINT '   ❌ FAIL: Duplicate business keys detected!';

PRINT '';

-- ============================================================================
-- 2. VERIFY CHANGEHASHKEY UNCHANGED FOR UNMODIFIED RECORDS
-- ============================================================================

PRINT '2. CHANGEHASHKEY STABILITY VALIDATION';
PRINT '';

DECLARE @ChangedHashCount INT;

SELECT @ChangedHashCount = COUNT(*)
FROM [test].[Customers] curr
JOIN #PreIdempotencySnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
WHERE curr.[ChangeHashKey] <> pre.OriginalHash;

PRINT '   Records with changed ChangeHashKey: ' + CAST(@ChangedHashCount AS NVARCHAR(10));

IF @ChangedHashCount = 0
    PRINT '   ✅ PASS: ChangeHashKey unchanged for all records';
ELSE
BEGIN
    PRINT '   ❌ FAIL: Some records have changed ChangeHashKey!';
    PRINT '';
    PRINT '   Records with changed hash:';
    SELECT TOP 5
        curr.[CustomerId],
        pre.OriginalHash AS PreviousHash,
        curr.[ChangeHashKey] AS CurrentHash
    FROM [test].[Customers] curr
    JOIN #PreIdempotencySnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
    WHERE curr.[ChangeHashKey] <> pre.OriginalHash
    ORDER BY curr.[CustomerId];
END

PRINT '';

-- ============================================================================
-- 3. VERIFY UPDATEDATETIME UNCHANGED FOR UNMODIFIED RECORDS
-- ============================================================================

PRINT '3. UPDATEDATETIME STABILITY VALIDATION';
PRINT '';

DECLARE @UpdatedDateCount INT;

SELECT @UpdatedDateCount = COUNT(*)
FROM [test].[Customers] curr
JOIN #PreIdempotencySnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
WHERE curr.[UpdateDatetime] <> pre.OriginalUpdateDatetime;

PRINT '   Records with changed UpdateDatetime: ' + CAST(@UpdatedDateCount AS NVARCHAR(10));

IF @UpdatedDateCount = 0
    PRINT '   ✅ PASS: UpdateDatetime unchanged (no unnecessary updates)';
ELSE
BEGIN
    PRINT '   ❌ FAIL: Some records have changed UpdateDatetime!';
    PRINT '';
    PRINT '   Records with updated datetime:';
    SELECT TOP 5
        curr.[CustomerId],
        pre.OriginalUpdateDatetime AS PreviousUpdateDatetime,
        curr.[UpdateDatetime] AS CurrentUpdateDatetime,
        DATEDIFF(SECOND, pre.OriginalUpdateDatetime, curr.[UpdateDatetime]) AS SecondsDifference
    FROM [test].[Customers] curr
    JOIN #PreIdempotencySnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
    WHERE curr.[UpdateDatetime] <> pre.OriginalUpdateDatetime
    ORDER BY curr.[CustomerId];
END

PRINT '';

-- ============================================================================
-- 4. VERIFY ISDELETED FLAG UNCHANGED
-- ============================================================================

PRINT '4. SOFT-DELETE FLAG STABILITY VALIDATION';
PRINT '';

DECLARE @ChangedDeletedFlagCount INT;

SELECT @ChangedDeletedFlagCount = COUNT(*)
FROM [test].[Customers] curr
JOIN #PreIdempotencySnapshot pre ON curr.[CustomerId] = pre.[CustomerId]
WHERE curr.[IsDeleted] <> pre.OriginalIsDeleted;

PRINT '   Records with changed IsDeleted flag: ' + CAST(@ChangedDeletedFlagCount AS NVARCHAR(10));

IF @ChangedDeletedFlagCount = 0
    PRINT '   ✅ PASS: IsDeleted flag unchanged';
ELSE
    PRINT '   ⚠️  WARNING: Some records have changed IsDeleted flag';

PRINT '';

-- ============================================================================
-- 5. PERFORMANCE METRICS (Optional)
-- ============================================================================

PRINT '5. PERFORMANCE METRICS';
PRINT '';
PRINT '   Check SSIS execution logs to verify:';
PRINT '   - Second execution should have 0 INSERTs (all records already exist)';
PRINT '   - Second execution should have 0 UPDATEs (no changes detected)';
PRINT '   - Execution time should be similar or faster (fewer operations)';
PRINT '';

-- Query _meta.SourceTables for last sync info
SELECT 
    st.LandingTableName,
    st.LastSyncAt,
    st.LastSyncStatus,
    st.LastSyncMessage
FROM _meta.SourceTables st
JOIN _meta.Sources s ON st.SourceId = s.Id
WHERE s.[Name] = 'TestSource'
  AND st.TableName = 'Customers';

PRINT '';
PRINT '============================================================================';
PRINT 'VALIDATION COMPLETED';
PRINT '============================================================================';
PRINT '';
PRINT '✅ All FASE 5 tests completed!';
PRINT '';
PRINT 'Review results and update FASE5_TestChecklist.md';

-- Cleanup
DROP TABLE #PreIdempotencySnapshot;

GO
*/
