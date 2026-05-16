# SQL Server Agent Integration — DW-Builder FASE 7

**Author:** db-developer  
**Date:** 2026-05-16  
**Version:** 1.0

---

## Overview

This folder contains all SQL Server Agent integration components for DW-Builder automated ETL scheduling. The system creates and manages SQL Server Agent jobs to execute SSIS packages on configurable schedules, with enhanced logging to track execution metrics and errors.

---

## Architecture

### Component Diagram

```
┌────────────────────────────────────────────────────────────────┐
│  DW-Builder Configuration (_meta schema)                      │
│                                                                │
│  SourceTables table:                                           │
│    - ScheduleEnabled (bit)                                     │
│    - ScheduleType (Daily/Weekly/Monthly/OnDemand)              │
│    - ScheduleTime (time)                                       │
│    - ScheduleDaysOfWeek (nvarchar)                             │
│    - ScheduleFrequency (int)                                   │
└────────────────────┬───────────────────────────────────────────┘
                     │
                     ▼
┌────────────────────────────────────────────────────────────────┐
│  CreateJobsForSource.sql (DDL Script)                          │
│                                                                │
│  Reads _meta.SourceTables where ScheduleEnabled = 1           │
│  Creates SQL Server Agent Jobs:                                │
│    - Job Name: DwBuilder_Source{n}_Table{n}_{TableName}       │
│    - Job Step: Execute SSIS package from SSISDB               │
│    - Schedule: Based on ScheduleType parameters               │
└────────────────────┬───────────────────────────────────────────┘
                     │
                     ▼
┌────────────────────────────────────────────────────────────────┐
│  SQL Server Agent (msdb)                                       │
│                                                                │
│  Jobs execute on schedule, trigger SSIS package:               │
│    SSISDB/DwBuilder/{SourceName}/{TableName}.dtsx             │
└────────────────────┬───────────────────────────────────────────┘
                     │
                     ▼
┌────────────────────────────────────────────────────────────────┐
│  SSIS Package Execution (SSISDB Catalog)                       │
│                                                                │
│  Package performs:                                             │
│    1. OLE DB Source (read from source table)                   │
│    2. Calculate ChangeHashKey (SHA-256)                        │
│    3. MERGE into landing table (INSERT/UPDATE/soft-delete)     │
└────────────────────┬───────────────────────────────────────────┘
                     │
                     ▼
┌────────────────────────────────────────────────────────────────┐
│  Enhanced Logging (_meta.Logs)                                 │
│                                                                │
│  usp_LogJobExecution writes:                                   │
│    - JobName, PackageName                                      │
│    - RowsInserted, RowsUpdated, RowsDeleted                    │
│    - ExecutionDurationMs                                       │
│    - ErrorDetails (if failed)                                  │
└────────────────────────────────────────────────────────────────┘
```

---

## Files in this Folder

| File | Description |
|---|---|
| `CreateJobsForSource.sql` | Main DDL script to create SQL Agent jobs for a source |
| `usp_LogJobExecution.sql` | Stored procedure for enhanced logging |
| `TestScenarios/01_CreateTestJobs.sql` | Test: Create jobs for SourceId=1 |
| `TestScenarios/02_ExecuteJobManually.sql` | Test: Manual job execution |
| `TestScenarios/03_QueryJobHistory.sql` | Test: Query job execution history |
| `TestScenarios/04_DisableEnableJobs.sql` | Test: Enable/disable jobs |
| `TestScenarios/05_DeleteJobs.sql` | Test: Delete test jobs |
| `TestScenarios/README.md` | Detailed test scenario documentation |
| `README.md` | This file |

---

## Prerequisites

### 1. SQL Server Agent

SQL Server Agent must be **running**:

```sql
-- Check status
EXEC xp_servicecontrol 'QueryState', N'SQLServerAGENT';
-- Expected: Running.

-- Start if stopped (requires sysadmin)
EXEC xp_servicecontrol 'Start', N'SQLServerAGENT';
```

### 2. SSIS Catalog (SSISDB)

SSIS Integration Services Catalog must be configured:

```sql
-- Check if SSISDB exists
SELECT name FROM sys.databases WHERE name = 'SSISDB';

-- If missing, create in SSMS:
-- Right-click "Integration Services Catalogs" > Create Catalog
-- Set password for SSISDB encryption key
```

**Note:** For file system deployment (alternative to SSISDB), see "Deployment Options" below.

### 3. SSIS Package Deployment

Packages must be deployed to SSISDB before jobs can execute them:

**Via SSDT (SQL Server Data Tools):**
1. Build SSIS project (`.ispac` file)
2. Right-click project > Deploy
3. Target: `SSISDB/DwBuilder/{SourceName}/`

**Via PowerShell:**
```powershell
# Example: Deploy .ispac to SSISDB
$ispac = "C:\SSIS\DwBuilder\ERP.ispac"
$serverName = "DWSERVER\DW"
$folderName = "DwBuilder"
$projectName = "ERP"

# Load Integration Services assembly
[Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.Management.IntegrationServices")

$conn = New-Object Microsoft.SqlServer.Management.IntegrationServices.IntegrationServices "Data Source=$serverName;Initial Catalog=SSISDB;Integrated Security=SSRS;"
$catalog = $conn.Catalogs["SSISDB"]
$folder = $catalog.Folders[$folderName]

# Create folder if not exists
if (!$folder) {
    $folder = New-Object Microsoft.SqlServer.Management.IntegrationServices.CatalogFolder($catalog, $folderName, "DW-Builder packages")
    $folder.Create()
}

# Deploy project
[byte[]]$projectFile = [System.IO.File]::ReadAllBytes($ispac)
$folder.DeployProject($projectName, $projectFile)

Write-Host "Project deployed successfully to SSISDB/$folderName/$projectName"
```

### 4. Database Migrations Applied

The following EF Core migrations must be applied:

- `AddSchedulingToSourceTables` — Adds scheduling columns to `_meta.SourceTables`
- `EnhanceLogsForJobTracking` — Adds job tracking columns to `_meta.Logs`

Apply migrations:
```bash
cd src/DwBuilder.Infrastructure
dotnet ef database update
```

### 5. Stored Procedure Created

Create the logging stored procedure:

```bash
# Via sqlcmd
sqlcmd -S DWSERVER\DW -d DwBuilderDW -i database\SqlAgent\usp_LogJobExecution.sql

# Or execute in SSMS
```

---

## Setup Instructions

### Step 1: Configure Scheduling for SourceTables

Update `_meta.SourceTables` to enable scheduling:

```sql
USE [DwBuilderDW];

-- Example: Enable daily sync at 2 AM for Customers table
UPDATE [_meta].[SourceTables]
SET 
    ScheduleEnabled = 1,
    ScheduleType = 'Daily',
    ScheduleTime = '02:00:00',
    ScheduleFrequency = 1, -- Every 1 day
    ScheduleDescription = 'Daily customer sync at 2 AM'
WHERE SourceId = 1 AND TableName = 'Customers';

-- Example: Enable weekly sync on Monday/Wednesday/Friday at 6 PM
UPDATE [_meta].[SourceTables]
SET 
    ScheduleEnabled = 1,
    ScheduleType = 'Weekly',
    ScheduleTime = '18:00:00',
    ScheduleDaysOfWeek = 'Monday,Wednesday,Friday',
    ScheduleDescription = 'Weekly orders sync MWF at 6 PM'
WHERE SourceId = 1 AND TableName = 'Orders';

-- Example: On-demand only (manual execution)
UPDATE [_meta].[SourceTables]
SET 
    ScheduleEnabled = 1,
    ScheduleType = 'OnDemand',
    ScheduleDescription = 'Manual execution only'
WHERE SourceId = 1 AND TableName = 'ProductCatalog';
```

**ScheduleType Options:**
- `Daily`: Runs every N days at specified time
- `Weekly`: Runs on specified days of week at specified time
- `Monthly`: Runs on specified day of month at specified time
- `OnDemand`: No automatic schedule (manual execution only)

### Step 2: Create SQL Server Agent Jobs

Execute `CreateJobsForSource.sql` with parameters:

```sql
-- Update these parameters in the script:
DECLARE @SourceId INT = 1; -- Your source ID
DECLARE @SsisCatalogFolder NVARCHAR(200) = N'DwBuilder'; -- SSISDB folder name

-- Then execute the entire script in SSMS
:r database\SqlAgent\CreateJobsForSource.sql
```

**What this script does:**
1. Reads all `SourceTables` where `SourceId = @SourceId` and `ScheduleEnabled = 1`
2. For each table, creates a SQL Server Agent job with:
   - Job name: `DwBuilder_Source{SourceId}_Table{TableId}_{TableName}`
   - One job step: Execute SSIS package from SSISDB
   - Schedule based on `ScheduleType`, `ScheduleTime`, `ScheduleDaysOfWeek`
   - Error handling and logging via `usp_LogJobExecution`
3. Jobs are created in **enabled** state (will run on schedule)

### Step 3: Verify Job Creation

```sql
-- List all DW-Builder jobs
SELECT 
    j.name AS JobName,
    CASE j.enabled WHEN 1 THEN 'Enabled' ELSE 'Disabled' END AS Status,
    s.name AS ScheduleName,
    CASE s.freq_type
        WHEN 4 THEN 'Daily'
        WHEN 8 THEN 'Weekly'
        WHEN 16 THEN 'Monthly'
        ELSE 'Other'
    END AS ScheduleType,
    j.date_created AS CreatedDate
FROM msdb.dbo.sysjobs j
LEFT JOIN msdb.dbo.sysjobschedules js ON j.job_id = js.job_id
LEFT JOIN msdb.dbo.sysschedules s ON js.schedule_id = s.schedule_id
WHERE j.name LIKE 'DwBuilder_%'
ORDER BY j.name;
```

### Step 4: Manual Test Execution (Optional)

Before waiting for scheduled execution, test jobs manually:

```sql
-- Execute a specific job
EXEC msdb.dbo.sp_start_job @job_name = N'DwBuilder_Source1_Table1_Customers';

-- Monitor execution
SELECT 
    j.name AS JobName,
    CASE h.run_status
        WHEN 0 THEN 'Failed'
        WHEN 1 THEN 'Succeeded'
        WHEN 2 THEN 'Retry'
        WHEN 3 THEN 'Canceled'
        WHEN 4 THEN 'In Progress'
    END AS Status,
    msdb.dbo.agent_datetime(h.run_date, h.run_time) AS ExecutionTime,
    h.message AS Message
FROM msdb.dbo.sysjobhistory h
INNER JOIN msdb.dbo.sysjobs j ON h.job_id = j.job_id
WHERE j.name = 'DwBuilder_Source1_Table1_Customers'
  AND h.step_id = 0 -- Overall job status
ORDER BY h.instance_id DESC;
```

### Step 5: Monitor Execution Logs

Query enhanced logs from `_meta.Logs`:

```sql
-- Recent job executions with metrics
SELECT TOP 20
    Timestamp,
    Level,
    JobName,
    PackageName,
    RowsInserted,
    RowsUpdated,
    RowsDeleted,
    ExecutionDurationMs,
    CASE WHEN Level = 'Error' THEN ErrorDetails ELSE NULL END AS ErrorDetails
FROM [_meta].[Logs]
WHERE JobName IS NOT NULL
ORDER BY Timestamp DESC;
```

---

## Deployment Options

### Option 1: SSISDB Catalog Deployment (Recommended)

**Pros:**
- Centralized package management
- Built-in execution logging and monitoring
- Parameter management via catalog
- Environment variables support

**Package Path Format:**
```
/DwBuilder/{SourceName}/{TableName}.dtsx
```

**Job Step Command (generated by CreateJobsForSource.sql):**
```sql
EXEC [SSISDB].[catalog].[create_execution]
    @package_name = N'Customers.dtsx',
    @folder_name = N'DwBuilder',
    @project_name = N'ERP',
    @execution_id = @execution_id OUTPUT;

EXEC [SSISDB].[catalog].[start_execution] @execution_id;
```

### Option 2: File System Deployment

**Pros:**
- No SSISDB required
- Simpler setup for small deployments

**Cons:**
- Less robust logging
- Manual parameter management
- Requires file system access from SQL Agent service account

**Package Path Format:**
```
C:\SSIS\DwBuilder\{SourceName}\{TableName}.dtsx
```

**Job Step Command (alternative):**
```sql
EXEC xp_cmdshell 'dtexec /FILE "C:\SSIS\DwBuilder\ERP\Customers.dtsx"';
```

**Required Configuration:**
1. Enable `xp_cmdshell`:
   ```sql
   EXEC sp_configure 'xp_cmdshell', 1;
   RECONFIGURE;
   ```
2. Grant SQL Server Agent service account read access to `C:\SSIS\DwBuilder\`
3. Modify `CreateJobsForSource.sql` to use file system paths

**Note:** This documentation focuses on **SSISDB deployment** as the recommended approach.

---

## Maintenance

### Updating Schedules

To modify an existing job's schedule:

**Option A: Recreate job**
1. Update `ScheduleType`, `ScheduleTime`, etc. in `_meta.SourceTables`
2. Re-run `CreateJobsForSource.sql` (idempotent; will drop and recreate jobs)

**Option B: Direct SQL Agent modification**
```sql
-- Example: Change schedule time to 3 AM
DECLARE @JobName NVARCHAR(200) = N'DwBuilder_Source1_Table1_Customers';
DECLARE @ScheduleId INT;

SELECT @ScheduleId = s.schedule_id
FROM msdb.dbo.sysjobs j
INNER JOIN msdb.dbo.sysjobschedules js ON j.job_id = js.job_id
INNER JOIN msdb.dbo.sysschedules s ON js.schedule_id = s.schedule_id
WHERE j.name = @JobName;

EXEC msdb.dbo.sp_update_schedule
    @schedule_id = @ScheduleId,
    @active_start_time = 030000; -- 03:00:00 in HHMMSS format
```

### Disabling Jobs Temporarily

```sql
-- Disable all DW-Builder jobs (e.g., during source system maintenance)
DECLARE @JobName NVARCHAR(200);

DECLARE job_cursor CURSOR FOR
SELECT name FROM msdb.dbo.sysjobs WHERE name LIKE 'DwBuilder_%';

OPEN job_cursor;
FETCH NEXT FROM job_cursor INTO @JobName;

WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC msdb.dbo.sp_update_job @job_name = @JobName, @enabled = 0;
    FETCH NEXT FROM job_cursor INTO @JobName;
END

CLOSE job_cursor;
DEALLOCATE job_cursor;
```

### Deleting Jobs

```sql
-- Delete all jobs for a specific source
DECLARE @JobName NVARCHAR(200);

DECLARE job_cursor CURSOR FOR
SELECT name FROM msdb.dbo.sysjobs WHERE name LIKE 'DwBuilder_Source1_%';

OPEN job_cursor;
FETCH NEXT FROM job_cursor INTO @JobName;

WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC msdb.dbo.sp_delete_job 
        @job_name = @JobName,
        @delete_history = 1,
        @delete_unused_schedule = 1;
    FETCH NEXT FROM job_cursor INTO @JobName;
END

CLOSE job_cursor;
DEALLOCATE job_cursor;
```

---

## Monitoring & Alerting

### Query Performance Dashboard

```sql
-- Job execution summary (last 7 days)
SELECT 
    j.name AS JobName,
    COUNT(*) AS Executions,
    SUM(CASE WHEN h.run_status = 1 THEN 1 ELSE 0 END) AS Successes,
    SUM(CASE WHEN h.run_status = 0 THEN 1 ELSE 0 END) AS Failures,
    AVG(l.ExecutionDurationMs) / 1000.0 AS AvgDurationSeconds,
    AVG(l.RowsInserted + l.RowsUpdated + l.RowsDeleted) AS AvgRowsProcessed
FROM msdb.dbo.sysjobs j
INNER JOIN msdb.dbo.sysjobhistory h ON j.job_id = h.job_id
LEFT JOIN [_meta].[Logs] l 
    ON l.JobName = j.name
    AND ABS(DATEDIFF(SECOND, msdb.dbo.agent_datetime(h.run_date, h.run_time), l.Timestamp)) < 60
WHERE j.name LIKE 'DwBuilder_%'
  AND h.step_id = 0
  AND msdb.dbo.agent_datetime(h.run_date, h.run_time) >= DATEADD(DAY, -7, GETDATE())
GROUP BY j.name
ORDER BY Failures DESC, AvgDurationSeconds DESC;
```

### Email Alerts on Failure (SQL Server Agent Operators)

1. **Configure Database Mail** (if not already set up):
   ```sql
   -- Enable Database Mail
   EXEC sp_configure 'Database Mail XPs', 1;
   RECONFIGURE;
   
   -- Create mail profile (execute in SSMS, not scripted here for brevity)
   -- Management > Database Mail > Configure Database Mail
   ```

2. **Create SQL Agent Operator:**
   ```sql
   EXEC msdb.dbo.sp_add_operator
       @name = N'DwBuilderAdmin',
       @enabled = 1,
       @email_address = N'dwbuilder-admin@company.com';
   ```

3. **Configure Job Notifications:**
   ```sql
   -- Example: Configure failure notification for a job
   EXEC msdb.dbo.sp_update_job
       @job_name = N'DwBuilder_Source1_Table1_Customers',
       @notify_level_email = 2, -- 0=Never, 1=On success, 2=On failure, 3=Always
       @notify_email_operator_name = N'DwBuilderAdmin';
   ```

---

## Troubleshooting

### Common Issues

| Issue | Symptoms | Solution |
|---|---|---|
| **SQL Agent not running** | Jobs don't execute on schedule | `EXEC xp_servicecontrol 'Start', N'SQLServerAGENT';` |
| **SSISDB not found** | Job fails: "Cannot find catalog" | Create SSIS Catalog in SSMS |
| **Package not deployed** | Job fails: "Package not found" | Deploy `.ispac` to SSISDB/{Folder}/{Project}/ |
| **Permission denied** | Job fails: "User does not have permission" | Grant `ssis_admin` role to SQL Agent service account |
| **Schedule not triggering** | Job never runs automatically | Verify job and schedule are both enabled |
| **Logs not populated** | `_meta.Logs` has no job entries | Verify `usp_LogJobExecution` exists and is called in job step |

### Diagnostic Queries

```sql
-- Check SQL Server Agent status
EXEC xp_servicecontrol 'QueryState', N'SQLServerAGENT';

-- Verify SSISDB exists
SELECT name FROM sys.databases WHERE name = 'SSISDB';

-- List deployed SSIS packages
SELECT f.name AS FolderName, p.name AS ProjectName, pkg.name AS PackageName
FROM SSISDB.catalog.folders f
INNER JOIN SSISDB.catalog.projects p ON f.folder_id = p.folder_id
INNER JOIN SSISDB.catalog.packages pkg ON p.project_id = pkg.project_id
WHERE f.name = 'DwBuilder';

-- Check if stored procedure exists
SELECT * FROM sys.procedures WHERE schema_id = SCHEMA_ID('_meta') AND name = 'usp_LogJobExecution';
```

---

## Security Considerations

### Principle of Least Privilege

1. **SQL Server Agent Service Account:**
   - Requires `ssis_admin` role for SSISDB execution
   - Requires `db_datareader`/`db_datawriter` on `DwBuilderDW`
   - Should **not** be `sa` or sysadmin (unless necessary)

2. **Linked Server Credentials:**
   - Connection to source systems should use dedicated ETL service accounts
   - Credentials encrypted in `_meta.Sources.ConnectionPasswordEncrypted`

3. **Job Ownership:**
   - Jobs created with `@owner_login_name = N'sa'` (can be changed to dedicated account)
   - Review with: `SELECT name, owner_sid FROM msdb.dbo.sysjobs WHERE name LIKE 'DwBuilder_%';`

### Encryption

- **SSISDB encryption:** Enabled by default (catalog creation sets encryption key)
- **Connection strings:** Stored encrypted in `_meta.Sources`
- **Logs:** `_meta.Logs` should **not** contain sensitive data (PII, credentials)

---

## References

- **Microsoft Docs - SQL Server Agent:** https://learn.microsoft.com/en-us/sql/ssms/agent/sql-server-agent
- **SSIS Catalog Stored Procedures:** https://learn.microsoft.com/en-us/sql/integration-services/system-stored-procedures/catalog-create-execution-ssisdb-database
- **Database Mail Configuration:** https://learn.microsoft.com/en-us/sql/relational-databases/database-mail/configure-database-mail
- **DW-Builder Requirements:** `requirements.md`
- **Main Documentation:** `Documentation-master.md`

---

**SQL Server Agent Integration Version:** 1.0  
**Last Updated:** 2026-05-16  
**Maintainer:** db-developer
