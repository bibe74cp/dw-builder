# FASE 5 — Test Checklist

## Overview

This checklist tracks the completion and validation of all FASE 5 test scenarios. Update each checkbox as you complete the corresponding step.

---

## Setup Ambiente

### Database Setup
- [ ] SQL Server container running (`docker compose up -d sqlserver`)
- [ ] Database `TestSourceDB` created
- [ ] Table `dbo.Customers` created with PK and sample data
- [ ] Table `dbo.Orders` created with FK to Customers
- [ ] 20 customer records inserted
- [ ] 30 order records inserted

### DW Metadata Configuration
- [ ] `_meta.Sources` entry created for `TestSource`
- [ ] `_meta.SourceTables` entry created for `dbo.Customers`
- [ ] `_meta.SourceFields` entries created (6 fields: 1 business key + 5 non-key)
- [ ] Metadata configuration validated with query

### Landing/Staging Tables
- [ ] Schema `[test]` created in `DwBuilderDW`
- [ ] Landing table `[test].[Customers]` created with standard structure
- [ ] Staging table `[test].[stg_Customers]` created (no PK)
- [ ] Table structures validated

---

## Compilazione BIML

### API and Tools
- [ ] DW-Builder API running (`docker compose up -d api`)
- [ ] JWT token obtained via `/api/v1/auth/login`
- [ ] Visual Studio 2022 installed with SSDT
- [ ] BimlExpress extension installed

### BIML File Download and Compilation
- [ ] BIML file downloaded from `GET /api/v1/biml` using PowerShell script
- [ ] Integration Services Project created in Visual Studio
- [ ] `MasterTemplate.biml` added to project
- [ ] BIML file compiled successfully with BimlExpress ("Expand Biml File")
- [ ] No compilation errors in Output window

### Generated SSIS Packages
- [ ] File `ETL_test_Customers.dtsx` generated
- [ ] File `Master_TestSource.dtsx` generated (if applicable)
- [ ] Packages can be opened in SSIS Designer
- [ ] Data Flow tasks have green validation icons

### Connection Managers Configuration
- [ ] Connection manager `TestSource_Connection` configured:
  - Server: `localhost,1433`
  - Database: `TestSourceDB`
  - User: `sa`
  - Password: from `.env.docker`
- [ ] Connection manager `DW_Connection` configured:
  - Server: `localhost,1433`
  - Database: `DwBuilderDW`
  - User: `sa`
  - Password: from `.env.docker`
- [ ] Both connections tested successfully

---

## Test Esecuzione SSIS

### Test #38: First Load (INSERT)

**Preparation:**
- [ ] Landing table truncated (`TRUNCATE TABLE [test].[Customers]`)
- [ ] Source table has 20 records

**Execution:**
- [ ] SSIS package `ETL_test_Customers.dtsx` executed successfully
- [ ] Package completed with green checkmarks (no errors)

**Validation (01_ValidateInsert.sql):**
- [ ] ✅ Record count: 20 records in landing table
- [ ] ✅ ChangeHashKey: All records have valid 64-char hash
- [ ] ✅ InsertDatetime: All records have populated datetime
- [ ] ✅ IsDeleted: All records have `IsDeleted = 0`
- [ ] ✅ UpdateDatetime = InsertDatetime (first load)

**Overall Result:**
- [ ] ✅ Test #38 PASSED
- [ ] ❌ Test #38 FAILED (describe issue: _________________)

---

### Test #39: Change Detection (UPDATE)

**Preparation (Part 1):**
- [ ] Script `02_ValidateUpdate.sql` Part 1 executed
- [ ] 3 customers modified in TestSourceDB:
  - CustomerId 1: Email changed
  - CustomerId 5: Email and City changed
  - CustomerId 10: CustomerName changed

**Execution:**
- [ ] SSIS package `ETL_test_Customers.dtsx` executed successfully
- [ ] Package completed with green checkmarks

**Validation (Part 2):**
- [ ] ✅ ChangeHashKey: 3 modified records have new hash
- [ ] ✅ UpdateDatetime: 3 modified records have updated datetime
- [ ] ✅ Unmodified records: 17 records have unchanged hash
- [ ] ✅ Business data: Email, City, CustomerName updated correctly

**Overall Result:**
- [ ] ✅ Test #39 PASSED
- [ ] ❌ Test #39 FAILED (describe issue: _________________)

---

### Test #40: Soft-Delete Detection

**Preparation (Part 1):**
- [ ] Script `03_ValidateSoftDelete.sql` Part 1 executed
- [ ] 2 customers deleted from TestSourceDB:
  - CustomerId 3
  - CustomerId 7

**Execution:**
- [ ] SSIS package `ETL_test_Customers.dtsx` executed successfully
- [ ] Package completed with green checkmarks

**Validation (Part 2):**
- [ ] ✅ IsDeleted flag: 2 deleted records have `IsDeleted = 1`
- [ ] ✅ UpdateDatetime: 2 deleted records have updated datetime
- [ ] ✅ ChangeHashKey: 2 deleted records have unchanged hash
- [ ] ✅ Record existence: 2 records still in landing table (not hard-deleted)
- [ ] ✅ Business data: CustomerCode, CustomerName, Email unchanged

**Overall Result:**
- [ ] ✅ Test #40 PASSED
- [ ] ❌ Test #40 FAILED (describe issue: _________________)

---

### Test #41: Idempotency (Double Execution)

**Preparation (Part 1):**
- [ ] Script `04_ValidateIdempotency.sql` Part 1 executed
- [ ] Snapshot created before double execution

**Execution:**
- [ ] SSIS package `ETL_test_Customers.dtsx` executed **FIRST TIME**
- [ ] Package completed with green checkmarks
- [ ] SSIS package `ETL_test_Customers.dtsx` executed **SECOND TIME** (immediately after)
- [ ] Package completed with green checkmarks

**Validation (Part 2):**
- [ ] ✅ No duplicates: Record count unchanged
- [ ] ✅ No duplicate business keys
- [ ] ✅ ChangeHashKey: All records have unchanged hash
- [ ] ✅ UpdateDatetime: All records have unchanged datetime
- [ ] ✅ IsDeleted: All records have unchanged flag
- [ ] ✅ Performance: Second execution had 0 INSERTs, 0 UPDATEs

**Overall Result:**
- [ ] ✅ Test #41 PASSED
- [ ] ❌ Test #41 FAILED (describe issue: _________________)

---

## Risultati Finali

### All Tests Summary
- [ ] ✅ All 4 tests passed
- [ ] ⚠️  Some tests failed (requires investigation)

### Documentation
- [ ] Screenshots of SSIS execution logs saved (optional)
- [ ] Validation script outputs saved to log files
- [ ] Test results documented in GitHub issue comments

### GitHub Issues
- [ ] Issue #36 closed (Test data setup)
- [ ] Issue #37 closed (BIML compilation instructions)
- [ ] Issue #38 closed (First load test)
- [ ] Issue #39 closed (Change detection test)
- [ ] Issue #40 closed (Soft-delete test)
- [ ] Issue #41 closed (Idempotency test)
- [ ] Issue for FASE 5 master closed with summary comment

---

## Notes and Observations

### Issues Encountered
<!-- Document any issues or unexpected behavior during testing -->

### Performance Metrics
<!-- Record execution times, row counts, or other metrics -->

### Recommendations
<!-- Any recommendations for production deployment or improvements -->

---

## Sign-off

**Tester Name:** _________________  
**Date:** _________________  
**Approved for Production:** [ ] YES  /  [ ] NO (requires fixes)

---

## Next Steps

After completing all tests:
1. Review all validation outputs for accuracy
2. Close GitHub issues with detailed comments
3. Update project README.md with FASE 5 completion
4. Prepare for FASE 6 (if applicable): Production deployment planning
