# SSIS Package Validation — README

## Overview

This folder contains SQL validation scripts to verify SSIS package execution results for the following test scenarios:
- **Test #38:** First load (INSERT all records)
- **Test #39:** Change detection (UPDATE modified records)
- **Test #40:** Soft-delete detection (mark deleted records)
- **Test #41:** Idempotency (double execution without duplicates)

---

## Prerequisites

1. **Test environment fully configured:**
   - ✅ TestSourceDB created with sample data
   - ✅ DwBuilderDW metadata configured
   - ✅ Landing and staging tables created
   - ✅ BIML file compiled successfully with BimlExpress
   - ✅ SSIS packages generated (.dtsx files)

2. **SSIS packages ready to execute:**
   - `ETL_test_Customers.dtsx` (individual table package)
   - `Master_TestSource.dtsx` (optional: master package)

3. **SQL Server connection:**
   - Server: `localhost,1433`
   - Database: `DwBuilderDW` (for validation scripts)
   - User: `sa` / Password: from `.env.docker`

---

## Test Execution Workflow

### Test #38: First Load (INSERT)

**Objective:** Validate initial data load from source to landing table.

**Steps:**
1. **Ensure landing table is empty:**
   ```sql
   TRUNCATE TABLE [test].[Customers];
   ```

2. **Execute SSIS package:**
   - Open `ETL_test_Customers.dtsx` in Visual Studio
   - Right-click → **Execute Package**
   - Wait for completion (green checkmarks)

3. **Run validation script:**
   ```powershell
   sqlcmd -S localhost,1433 -U sa -P <SA_PASSWORD> -d DwBuilderDW -i "01_ValidateInsert.sql" -C
   ```

**Expected results:**
- ✅ 20 records inserted into `[test].[Customers]`
- ✅ All records have valid `ChangeHashKey` (SHA-256, 64 chars)
- ✅ `InsertDatetime` populated for all records
- ✅ `IsDeleted = 0` for all records
- ✅ `UpdateDatetime = InsertDatetime` (first load)

---

### Test #39: Change Detection (UPDATE)

**Objective:** Validate SSIS package detects changes and updates `ChangeHashKey`.

**Steps:**
1. **Run Part 1 (modify source data):**
   ```powershell
   sqlcmd -S localhost,1433 -U sa -P <SA_PASSWORD> -d DwBuilderDW -i "02_ValidateUpdate.sql" -C
   ```
   This modifies 3 customers in TestSourceDB:
   - CustomerId 1: Email changed
   - CustomerId 5: Email and City changed
   - CustomerId 10: CustomerName changed

2. **Execute SSIS package** (same as Test #38)

3. **Run Part 2 (validation):**
   - Uncomment Part 2 in `02_ValidateUpdate.sql`
   - Execute script

**Expected results:**
- ✅ 3 modified records have **new** `ChangeHashKey`
- ✅ 3 modified records have **updated** `UpdateDatetime`
- ✅ 17 unmodified records have **unchanged** hash and datetime
- ✅ Business data (Email, City, CustomerName) updated correctly

---

### Test #40: Soft-Delete Detection

**Objective:** Validate SSIS package marks deleted records with `IsDeleted = 1`.

**Steps:**
1. **Run Part 1 (delete source records):**
   ```powershell
   sqlcmd -S localhost,1433 -U sa -P <SA_PASSWORD> -d DwBuilderDW -i "03_ValidateSoftDelete.sql" -C
   ```
   This deletes 2 customers from TestSourceDB:
   - CustomerId 3
   - CustomerId 7

2. **Execute SSIS package** (same as Test #38)

3. **Run Part 2 (validation):**
   - Uncomment Part 2 in `03_ValidateSoftDelete.sql`
   - Execute script

**Expected results:**
- ✅ 2 deleted records have `IsDeleted = 1`
- ✅ 2 deleted records have **updated** `UpdateDatetime`
- ✅ 2 deleted records have **unchanged** `ChangeHashKey`
- ✅ Records still exist in landing table (soft-delete, not hard-delete)
- ✅ Business data (CustomerCode, CustomerName, Email) unchanged

---

### Test #41: Idempotency (Double Execution)

**Objective:** Validate SSIS package can run twice without creating duplicates.

**Steps:**
1. **Run Part 1 (capture state):**
   ```powershell
   sqlcmd -S localhost,1433 -U sa -P <SA_PASSWORD> -d DwBuilderDW -i "04_ValidateIdempotency.sql" -C
   ```

2. **Execute SSIS package TWICE consecutively:**
   - First execution → wait for completion
   - Second execution immediately after → wait for completion

3. **Run Part 2 (validation):**
   - Uncomment Part 2 in `04_ValidateIdempotency.sql`
   - Execute script

**Expected results:**
- ✅ Record count unchanged (no duplicates)
- ✅ No duplicate business keys
- ✅ `ChangeHashKey` unchanged for all records
- ✅ `UpdateDatetime` unchanged for all records (no unnecessary updates)
- ✅ `IsDeleted` flag unchanged
- ✅ Second execution has 0 INSERTs, 0 UPDATEs (check SSIS logs)

---

## Validation Output Format

Each validation script outputs:
- Section-by-section validation results
- ✅ PASS or ❌ FAIL for each check
- Sample data rows for inspection
- Detailed mismatch information if validation fails

**Example output:**
```
============================================================================
SSIS VALIDATION — TEST #38: First Load (INSERT)
============================================================================

1. RECORD COUNT VALIDATION
   Source table: 20 records
   Landing table: 20 records
   ✅ PASS: Record counts match

2. CHANGEHASHKEY VALIDATION
   Records with NULL or empty hash: 0
   ✅ PASS: All records have valid ChangeHashKey

...
```

---

## Troubleshooting

### Issue 1: SSIS Package Execution Fails

**Symptom:** Red error icon in SSIS Designer

**Common causes:**
- Connection managers not configured (see `../BimlCompilation/README.md`)
- Source database not accessible
- Permissions issue

**Solution:**
1. Check SSIS execution log (Output window)
2. Verify connection strings in Connection Managers
3. Test connections: right-click → **Edit** → **Test Connection**

---

### Issue 2: Validation Script Fails with "Object not found"

**Symptom:** `Invalid object name 'test.Customers'`

**Solution:**
Execute `../TestData/04_CreateLandingAndStagingTables.sql` to create landing tables.

---

### Issue 3: Record Count Mismatch

**Symptom:** Landing table has fewer records than source

**Common causes:**
- SSIS package terminated early (check for errors)
- Data Flow task not connected to staging table
- MERGE statement failed

**Solution:**
1. Re-execute SSIS package with breakpoints on Data Flow
2. Check staging table before MERGE:
   ```sql
   SELECT COUNT(*) FROM [test].[stg_Customers];
   ```
3. If staging table is empty, check OLE DB Destination configuration

---

### Issue 4: ChangeHashKey All Identical

**Symptom:** All records have the same hash value

**Cause:** Script Component C# code not working correctly (concatenation issue)

**Solution:**
1. Open SSIS package in Visual Studio
2. Double-click Script Component in Data Flow
3. Review C# code for hash calculation
4. Ensure all non-key columns are concatenated with `|` separator
5. Re-compile and re-execute

---

## Performance Metrics

Expected execution times (on test dataset of 20 records):
- First load: ~1-2 seconds
- Update (3 changes): ~1-2 seconds
- Soft-delete (2 deletes): ~1-2 seconds
- Idempotency (no changes): ~1 second (faster, no UPDATEs)

For larger datasets (10K+ records), monitor:
- Data Flow throughput (rows/sec)
- MERGE statement execution time
- Index seek vs scan on business keys

---

## Next Steps

After completing all 4 tests:
1. Update `../FASE5_TestChecklist.md` with results
2. Take screenshots of SSIS execution logs (optional)
3. Close GitHub issues #38, #39, #40, #41
4. Prepare for production deployment (if all tests pass)

---

## Additional Resources

- [SSIS Best Practices](https://docs.microsoft.com/en-us/sql/integration-services/integration-services-best-practices)
- [SQL Server MERGE Statement](https://docs.microsoft.com/en-us/sql/t-sql/statements/merge-transact-sql)
- [DW-Builder Project Documentation](../../Documentation-master.md)
