/*******************************************************************************
 * File:         02_ExecuteJobManually.sql
 * Author:       db-developer
 * Date:         2026-05-16
 * Description:  Executes a SQL Server Agent job manually for testing purposes.
 *               
 * Prerequisites: - Jobs created via CreateJobsForSource.sql
 *                - SQL Server Agent service is running
 *                
 * Usage:        1. Update @JobName with the specific job to execute
 *               2. Execute script in SSMS
 *               3. Monitor job history and _meta.Logs
 ******************************************************************************/

USE [DwBuilderDW];
GO

SET NOCOUNT ON;
GO

PRINT '========================================';
PRINT 'TEST SCENARIO 2: Execute Job Manually';
PRINT 'Timestamp: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '========================================';
PRINT '';

-- Specify job to execute
DECLARE @JobName NVARCHAR(200) = N'DwBuilder_Source1_Table1_Customers'; -- UPDATE THIS
DECLARE @JobId UNIQUEIDENTIFIER;

-- Verify job exists
SELECT @JobId = job_id
FROM msdb.dbo.sysjobs
WHERE name = @JobName;

IF @JobId IS NULL
BEGIN
    RAISERROR('Job "%s" not found. Available jobs:', 16, 1, @JobName);
    
    SELECT name AS AvailableJobs
    FROM msdb.dbo.sysjobs
    WHERE name LIKE 'DwBuilder_%'
    ORDER BY name;
    
    RETURN;
END

PRINT 'Job found: ' + @JobName;
PRINT 'Job ID: ' + CAST(@JobId AS NVARCHAR(50));
PRINT '';

-- Check if job is currently running
IF EXISTS (
    SELECT 1
    FROM msdb.dbo.sysjobactivity ja
    INNER JOIN msdb.dbo.sysjobs j ON ja.job_id = j.job_id
    WHERE ja.stop_execution_date IS NULL
      AND ja.start_execution_date IS NOT NULL
      AND j.job_id = @JobId
)
BEGIN
    PRINT 'WARNING: Job is currently running. Waiting for completion...';
    PRINT '';
    
    -- Wait for current execution to complete (timeout after 5 minutes)
    DECLARE @WaitCount INT = 0;
    WHILE @WaitCount < 60
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM msdb.dbo.sysjobactivity ja
            WHERE ja.job_id = @JobId
              AND ja.stop_execution_date IS NULL
              AND ja.start_execution_date IS NOT NULL
        )
            BREAK;
        
        WAITFOR DELAY '00:00:05';
        SET @WaitCount = @WaitCount + 1;
    END
    
    IF @WaitCount >= 60
    BEGIN
        RAISERROR('Job is still running after 5 minutes. Aborting test.', 16, 1);
        RETURN;
    END
END

-- Start job execution
PRINT 'Starting job execution...';
PRINT '';

EXEC msdb.dbo.sp_start_job @job_name = @JobName;

PRINT 'Job started successfully.';
PRINT 'Monitoring execution status...';
PRINT '';

-- Poll for completion (timeout after 10 minutes)
DECLARE @MaxWait INT = 120; -- 10 minutes (120 * 5 seconds)
DECLARE @WaitCounter INT = 0;
DECLARE @RunStatus INT;

WHILE @WaitCounter < @MaxWait
BEGIN
    -- Check latest run status
    SELECT TOP 1 @RunStatus = run_status
    FROM msdb.dbo.sysjobhistory
    WHERE job_id = @JobId
      AND step_id = 0 -- Overall job status
    ORDER BY instance_id DESC;
    
    -- Check if job is still running
    IF NOT EXISTS (
        SELECT 1
        FROM msdb.dbo.sysjobactivity ja
        WHERE ja.job_id = @JobId
          AND ja.stop_execution_date IS NULL
          AND ja.start_execution_date IS NOT NULL
    )
    BEGIN
        PRINT 'Job execution completed.';
        BREAK;
    END
    
    PRINT 'Still running... (' + CAST((@WaitCounter * 5) AS NVARCHAR(10)) + ' seconds elapsed)';
    WAITFOR DELAY '00:00:05';
    SET @WaitCounter = @WaitCounter + 1;
END

IF @WaitCounter >= @MaxWait
BEGIN
    PRINT 'WARNING: Job is still running after 10 minutes. Check SQL Server Agent Job Activity Monitor.';
END

PRINT '';
PRINT 'Job execution status:';

-- Display latest job run history
SELECT TOP 1
    j.name AS JobName,
    CASE h.run_status
        WHEN 0 THEN 'Failed'
        WHEN 1 THEN 'Succeeded'
        WHEN 2 THEN 'Retry'
        WHEN 3 THEN 'Canceled'
        WHEN 4 THEN 'In Progress'
    END AS RunStatus,
    msdb.dbo.agent_datetime(h.run_date, h.run_time) AS ExecutionTime,
    STUFF(STUFF(STUFF(RIGHT('000000' + CAST(h.run_duration AS VARCHAR(6)), 6), 5, 0, ':'), 3, 0, ':'), 1, 0, '') AS Duration,
    h.message AS Message
FROM msdb.dbo.sysjobhistory h
INNER JOIN msdb.dbo.sysjobs j ON h.job_id = j.job_id
WHERE h.job_id = @JobId
  AND h.step_id = 0 -- Overall job status
ORDER BY h.instance_id DESC;

PRINT '';
PRINT 'Checking _meta.Logs for execution record:';

-- Display log entry created by usp_LogJobExecution
SELECT TOP 5
    Id,
    Timestamp,
    Level,
    Message,
    JobName,
    PackageName,
    RowsInserted,
    RowsUpdated,
    RowsDeleted,
    ExecutionDurationMs,
    ErrorDetails
FROM [_meta].[Logs]
WHERE JobName = @JobName
ORDER BY Timestamp DESC;

PRINT '';
PRINT '========================================';
PRINT 'Manual job execution test completed.';
PRINT '========================================';
GO
