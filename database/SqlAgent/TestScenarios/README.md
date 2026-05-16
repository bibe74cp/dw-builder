# SQL Server Agent Testing — Test Scenarios

**Author:** db-developer  
**Date:** 2026-05-16  
**Project:** DW-Builder FASE 7

---

## Overview

This folder contains comprehensive test scenarios for validating SQL Server Agent integration with DW-Builder. The tests cover job creation, manual execution, history monitoring, enable/disable operations, and cleanup.

---

## Prerequisites

Before running these test scenarios, ensure:

1. ✅ **Database setup complete:**
   - `DwBuilderDW` database exists
   - Schema `_meta` with tables `Sources`, `SourceTables`, `Logs`
   - Migration scripts applied (`AddSchedulingToSourceTables`, `EnhanceLogsForJobTracking`)

2. ✅ **SQL Server Agent configured:**
   - SQL Server Agent service is **running**
   - Check: `EXEC xp_servicecontrol 'QueryState', N'SQLServerAGENT';`
   - Start if needed: `EXEC xp_servicecontrol 'Start', N'SQLServerAGENT';`

3. ✅ **SSIS environment ready:**
   - **SSIS Catalog (SSISDB)** created and configured
   - SSIS packages deployed to `SSISDB/DwBuilder/{SourceName}/{TableName}.dtsx`
   - Or: File system deployment with packages at `C:\SSIS\DwBuilder\{SourceName}\{TableName}.dtsx`

4. ✅ **Stored procedure created:**
   - `[_meta].[usp_LogJobExecution]` exists (run `usp_LogJobExecution.sql`)

5. ✅ **Test data configured:**
   - At least one `Source` record in `_meta.Sources` (e.g., SourceId=1)
   - At least one `SourceTable` with `ScheduleEnabled = 1`

---

## Test Scenarios

### 01_CreateTestJobs.sql

**Purpose:** Creates SQL Server Agent jobs for all enabled tables in a test source (SourceId=1).

**Execution:**
```sql
-- In SSMS, connect to DwBuilderDW database
-- Execute: database\SqlAgent\CreateJobsForSource.sql with @SourceId = 1

-- Then run verification:
:r 01_CreateTestJobs.sql
```

**Expected Results:**
- Jobs created with naming pattern: `DwBuilder_Source1_Table{n}_{TableName}`
- Each job has one step: "Execute SSIS Package"
- Schedules configured based on `ScheduleType`, `ScheduleTime`, etc.
- Jobs visible in SSMS under: `SQL Server Agent > Jobs`

**Validation Queries:**
```sql
-- List created jobs
SELECT name, enabled, date_created
FROM msdb.dbo.sysjobs
WHERE name LIKE 'DwBuilder_Source1_%';

-- Verify job steps
SELECT j.name, s.step_name, s.subsystem, s.database_name
FROM msdb.dbo.sysjobs j
INNER JOIN msdb.dbo.sysjobsteps s ON j.job_id = s.job_id
WHERE j.name LIKE 'DwBuilder_Source1_%';
```

---

### 02_ExecuteJobManually.sql

**Purpose:** Executes a specific SQL Server Agent job manually and monitors its completion.

**Configuration:**
Update `@JobName` variable in the script:
```sql
DECLARE @JobName NVARCHAR(200) = N'DwBuilder_Source1_Table1_Customers';
```

**Execution:**
```sql
:r 02_ExecuteJobManually.sql
```

**Expected Results:**
- Job starts execution
- Script polls for completion (timeout: 10 minutes)
- Final status displayed from `msdb.dbo.sysjobhistory`
- Execution log entry created in `_meta.Logs` via `usp_LogJobExecution`

**Troubleshooting:**
- If job fails, check: `_meta.Logs` for `ErrorDetails`
- Check SSISDB execution logs: `SELECT * FROM SSISDB.catalog.executions ORDER BY start_time DESC;`
- Review job step output: Right-click job in SSMS > View History

---

### 03_QueryJobHistory.sql

**Purpose:** Comprehensive queries to analyze job execution history with correlation to enhanced logging in `_meta.Logs`.

**Execution:**
```sql
:r 03_QueryJobHistory.sql
```

**Output Sections:**
1. **Job Execution Summary** — Success/failure counts, average duration (last 30 days)
2. **Recent Job Executions** — Last 50 runs with status and duration
3. **Enhanced Logging** — Detailed metrics from `_meta.Logs` (rows inserted/updated/deleted)
4. **Combined View** — SQL Agent history joined with `_meta.Logs` (timestamp correlation)
5. **Failure Analysis** — Failed jobs with error details

**Use Cases:**
- Performance monitoring: identify slow-running jobs
- Reliability tracking: success rate over time
- Troubleshooting: correlate SQL Agent errors with SSIS package errors

---

### 04_DisableEnableJobs.sql

**Purpose:** Disable or enable SQL Server Agent jobs for maintenance windows or troubleshooting.

**Configuration:**
Set `@Action` variable:
```sql
DECLARE @Action NVARCHAR(10) = 'STATUS'; -- OPTIONS: 'DISABLE', 'ENABLE', 'STATUS'
```

**Execution:**
```sql
-- Check current status
:r 04_DisableEnableJobs.sql

-- Disable all test jobs (update @Action to 'DISABLE' first)
-- Re-enable (update @Action to 'ENABLE')
```

**Expected Results:**
- `STATUS`: Lists all matching jobs with current enabled/disabled state
- `DISABLE`: Sets `enabled = 0` for all matching jobs
- `ENABLE`: Sets `enabled = 1` for all matching jobs

**Notes:**
- Disabling a job does **not** stop currently running executions
- Schedules remain configured; jobs won't run automatically while disabled
- Use during: maintenance windows, troubleshooting, source system outages

---

### 05_DeleteJobs.sql

**Purpose:** Permanently deletes SQL Server Agent jobs created during testing.

**⚠️ WARNING:** This is a destructive operation. Jobs and their execution history will be permanently removed.

**Configuration:**
```sql
DECLARE @DryRun BIT = 1; -- Set to 0 to actually delete
DECLARE @JobNamePattern NVARCHAR(200) = 'DwBuilder_Source1_%';
```

**Execution:**
```sql
-- Step 1: Dry run (review jobs to be deleted)
:r 05_DeleteJobs.sql

-- Step 2: Set @DryRun = 0 in the script, then execute again
```

**Expected Results:**
- Dry run mode: Lists jobs that would be deleted (no changes)
- Delete mode: Removes jobs and associated schedules
- Job history in `msdb.dbo.sysjobhistory` is also deleted
- Execution logs in `_meta.Logs` remain (for audit trail)

**Cleanup:**
To also remove log entries:
```sql
DELETE FROM [_meta].[Logs] WHERE JobName LIKE 'DwBuilder_Source1_%';
```

---

## Test Workflow (End-to-End)

### Step 1: Initial Setup
```sql
-- 1. Verify SQL Server Agent is running
EXEC xp_servicecontrol 'QueryState', N'SQLServerAGENT';

-- 2. Create stored procedure for logging
:r ..\usp_LogJobExecution.sql

-- 3. Configure test SourceTable with scheduling
UPDATE [_meta].[SourceTables]
SET 
    ScheduleEnabled = 1,
    ScheduleType = 'Daily',
    ScheduleTime = '02:00:00', -- 2 AM
    ScheduleDescription = 'Daily sync at 2 AM'
WHERE SourceId = 1 AND TableName = 'Customers';
```

### Step 2: Create Jobs
```sql
-- Execute CreateJobsForSource.sql with @SourceId = 1
-- Or run test script:
:r 01_CreateTestJobs.sql
```

### Step 3: Manual Execution Test
```sql
-- Update job name in script, then execute:
:r 02_ExecuteJobManually.sql
```

### Step 4: Monitor & Analyze
```sql
:r 03_QueryJobHistory.sql
```

### Step 5: Maintenance Operations
```sql
-- Disable jobs during maintenance
:r 04_DisableEnableJobs.sql -- Set @Action = 'DISABLE'

-- Re-enable after maintenance
:r 04_DisableEnableJobs.sql -- Set @Action = 'ENABLE'
```

### Step 6: Cleanup
```sql
-- Delete test jobs
:r 05_DeleteJobs.sql -- Set @DryRun = 0 after reviewing
```

---

## Common Issues & Troubleshooting

### Issue 1: Jobs not created
**Symptoms:** `CreateJobsForSource.sql` completes but no jobs appear in SQL Server Agent.

**Solutions:**
- Verify SQL Server Agent is running: `EXEC xp_servicecontrol 'QueryState', N'SQLServerAGENT';`
- Check for errors in script output
- Verify `ScheduleEnabled = 1` for target SourceTables
- Ensure user has `sysadmin` or `SQLAgentOperatorRole` permission

### Issue 2: Job execution fails immediately
**Symptoms:** Job starts but fails within seconds.

**Solutions:**
- Check `_meta.Logs` for `ErrorDetails`
- Verify SSIS Catalog path: `SELECT * FROM SSISDB.catalog.folders;`
- Confirm packages are deployed: `SELECT * FROM SSISDB.catalog.packages;`
- Test SSIS execution manually in SSDT or via `dtexec`
- Check SSISDB execution report: Right-click execution > All Messages

### Issue 3: usp_LogJobExecution not being called
**Symptoms:** Jobs run successfully but no entries in `_meta.Logs`.

**Solutions:**
- Verify stored procedure exists: `SELECT * FROM sys.procedures WHERE name = 'usp_LogJobExecution';`
- Check job step T-SQL includes `EXEC [_meta].[usp_LogJobExecution]`
- Review SQL Server Agent job history for step errors
- Ensure database context is `DwBuilderDW` in job step

### Issue 4: Schedule not triggering automatically
**Symptoms:** Manual execution works, but scheduled runs don't occur.

**Solutions:**
- Verify job is enabled: `SELECT name, enabled FROM msdb.dbo.sysjobs WHERE name LIKE 'DwBuilder_%';`
- Check schedule is enabled: `SELECT * FROM msdb.dbo.sysschedules WHERE name LIKE 'DwBuilder_%';`
- Confirm schedule time hasn't passed for today
- Check SQL Server Agent service account has appropriate permissions
- Review SQL Server Agent error log: `EXEC xp_readerrorlog 0, 2;` (2 = SQL Agent log)

---

## Performance Benchmarks

Expected execution times (reference hardware: SQL Server 2019, 8 vCPU, 16 GB RAM):

| Scenario | Duration | Notes |
|---|---|---|
| Job creation (10 tables) | < 5 seconds | One-time setup per source |
| Single job execution | 10-60 seconds | Varies by table size and network latency |
| Query job history | < 1 second | Indexed queries on msdb and _meta.Logs |
| Disable/Enable (10 jobs) | < 2 seconds | Metadata updates only |
| Delete jobs (10 jobs) | < 3 seconds | Includes history deletion |

---

## Next Steps

After completing these test scenarios:

1. ✅ **Production deployment:**
   - Review and adjust schedules for production workload
   - Configure error notifications (SQL Agent Operators + email alerts)
   - Set up monitoring dashboards using queries from `03_QueryJobHistory.sql`

2. ✅ **Integration with DW-Builder Web App:**
   - Implement API endpoints to create/update/delete jobs programmatically
   - Add UI for schedule configuration (currently done via direct SQL)
   - Build job monitoring dashboard in React frontend

3. ✅ **Enhanced logging:**
   - Extend SSIS packages to return row counts to SQL Agent (output parameters)
   - Implement custom event handlers in SSIS for granular logging
   - Set up alerting on consecutive failures

---

## References

- **SQL Server Agent Documentation:** [Microsoft Docs - SQL Server Agent](https://learn.microsoft.com/en-us/sql/ssms/agent/sql-server-agent)
- **SSIS Catalog Stored Procedures:** [Microsoft Docs - catalog.create_execution](https://learn.microsoft.com/en-us/sql/integration-services/system-stored-procedures/catalog-create-execution-ssisdb-database)
- **DW-Builder Requirements:** `requirements.md`
- **Main Documentation:** `Documentation-master.md` → Section "SQL Server Agent Scheduling"

---

**Test Scenarios Version:** 1.0  
**Last Updated:** 2026-05-16  
**Author:** db-developer
