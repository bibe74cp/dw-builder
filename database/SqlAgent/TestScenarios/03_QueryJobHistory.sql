/*******************************************************************************
 * File:         03_QueryJobHistory.sql
 * Author:       db-developer
 * Date:         2026-05-16
 * Description:  Queries SQL Server Agent job history with correlation to 
 *               _meta.Logs enhanced logging.
 *               
 * Prerequisites: - Jobs have been executed at least once
 *                
 * Usage:        Execute in SSMS to review job execution history
 ******************************************************************************/

USE [DwBuilderDW];
GO

SET NOCOUNT ON;
GO

PRINT '========================================';
PRINT 'TEST SCENARIO 3: Query Job History';
PRINT 'Timestamp: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '========================================';
PRINT '';

-- 1. Overall job execution summary
PRINT '--- 1. Job Execution Summary (Last 30 days) ---';
PRINT '';

SELECT 
    j.name AS JobName,
    COUNT(*) AS ExecutionCount,
    SUM(CASE WHEN h.run_status = 1 THEN 1 ELSE 0 END) AS SuccessCount,
    SUM(CASE WHEN h.run_status = 0 THEN 1 ELSE 0 END) AS FailureCount,
    MAX(msdb.dbo.agent_datetime(h.run_date, h.run_time)) AS LastExecution,
    AVG(
        CAST(SUBSTRING(CAST(h.run_duration AS VARCHAR(6)), 1, 2) AS INT) * 3600 +
        CAST(SUBSTRING(CAST(h.run_duration AS VARCHAR(6)), 3, 2) AS INT) * 60 +
        CAST(SUBSTRING(CAST(h.run_duration AS VARCHAR(6)), 5, 2) AS INT)
    ) AS AvgDurationSeconds
FROM msdb.dbo.sysjobhistory h
INNER JOIN msdb.dbo.sysjobs j ON h.job_id = j.job_id
WHERE j.name LIKE 'DwBuilder_%'
  AND h.step_id = 0 -- Overall job status only
  AND msdb.dbo.agent_datetime(h.run_date, h.run_time) >= DATEADD(DAY, -30, GETDATE())
GROUP BY j.name
ORDER BY LastExecution DESC;

PRINT '';
PRINT '--- 2. Recent Job Executions (Last 50) ---';
PRINT '';

-- 2. Recent job executions with details
SELECT TOP 50
    j.name AS JobName,
    CASE h.run_status
        WHEN 0 THEN 'Failed'
        WHEN 1 THEN 'Succeeded'
        WHEN 2 THEN 'Retry'
        WHEN 3 THEN 'Canceled'
        WHEN 4 THEN 'In Progress'
    END AS Status,
    msdb.dbo.agent_datetime(h.run_date, h.run_time) AS ExecutionTime,
    STUFF(STUFF(STUFF(RIGHT('000000' + CAST(h.run_duration AS VARCHAR(6)), 6), 5, 0, ':'), 3, 0, ':'), 1, 0, '') AS Duration,
    CASE 
        WHEN h.run_status = 0 THEN h.message
        ELSE NULL
    END AS ErrorMessage
FROM msdb.dbo.sysjobhistory h
INNER JOIN msdb.dbo.sysjobs j ON h.job_id = j.job_id
WHERE j.name LIKE 'DwBuilder_%'
  AND h.step_id = 0
ORDER BY h.instance_id DESC;

PRINT '';
PRINT '--- 3. Enhanced Logging from _meta.Logs (Last 50 entries) ---';
PRINT '';

-- 3. Enhanced logging with row counts and duration
SELECT TOP 50
    Timestamp,
    Level,
    JobName,
    PackageName,
    RowsInserted,
    RowsUpdated,
    RowsDeleted,
    ExecutionDurationMs,
    CASE 
        WHEN Level = 'Error' THEN ErrorDetails
        ELSE NULL
    END AS ErrorDetails
FROM [_meta].[Logs]
WHERE JobName IS NOT NULL
ORDER BY Timestamp DESC;

PRINT '';
PRINT '--- 4. Combined View: SQL Agent + Enhanced Logs ---';
PRINT '';

-- 4. Combined view with JOIN (where available)
-- Note: Direct correlation requires JobExecutionId tracking or timestamp proximity
SELECT 
    j.name AS JobName,
    CASE h.run_status
        WHEN 0 THEN 'Failed'
        WHEN 1 THEN 'Succeeded'
        ELSE 'Other'
    END AS AgentStatus,
    msdb.dbo.agent_datetime(h.run_date, h.run_time) AS AgentExecutionTime,
    STUFF(STUFF(STUFF(RIGHT('000000' + CAST(h.run_duration AS VARCHAR(6)), 6), 5, 0, ':'), 3, 0, ':'), 1, 0, '') AS AgentDuration,
    l.Level AS LogLevel,
    l.Timestamp AS LogTimestamp,
    l.RowsInserted,
    l.RowsUpdated,
    l.RowsDeleted,
    l.ExecutionDurationMs,
    l.ErrorDetails
FROM msdb.dbo.sysjobhistory h
INNER JOIN msdb.dbo.sysjobs j ON h.job_id = j.job_id
LEFT JOIN [_meta].[Logs] l 
    ON l.JobName = j.name
    AND ABS(DATEDIFF(SECOND, msdb.dbo.agent_datetime(h.run_date, h.run_time), l.Timestamp)) < 60 -- Match within 1 minute
WHERE j.name LIKE 'DwBuilder_%'
  AND h.step_id = 0
  AND msdb.dbo.agent_datetime(h.run_date, h.run_time) >= DATEADD(DAY, -7, GETDATE())
ORDER BY AgentExecutionTime DESC;

PRINT '';
PRINT '--- 5. Failure Analysis (Last 30 days) ---';
PRINT '';

-- 5. Failed jobs with error details
SELECT 
    j.name AS JobName,
    msdb.dbo.agent_datetime(h.run_date, h.run_time) AS FailureTime,
    h.message AS AgentErrorMessage,
    l.ErrorDetails AS EnhancedErrorDetails
FROM msdb.dbo.sysjobhistory h
INNER JOIN msdb.dbo.sysjobs j ON h.job_id = j.job_id
LEFT JOIN [_meta].[Logs] l 
    ON l.JobName = j.name
    AND l.Level = 'Error'
    AND ABS(DATEDIFF(SECOND, msdb.dbo.agent_datetime(h.run_date, h.run_time), l.Timestamp)) < 60
WHERE j.name LIKE 'DwBuilder_%'
  AND h.run_status = 0 -- Failed
  AND h.step_id = 0
  AND msdb.dbo.agent_datetime(h.run_date, h.run_time) >= DATEADD(DAY, -30, GETDATE())
ORDER BY FailureTime DESC;

PRINT '';
PRINT '========================================';
PRINT 'Job history query completed.';
PRINT '========================================';
GO
