/*******************************************************************************
 * File:         04_DisableEnableJobs.sql
 * Author:       db-developer
 * Date:         2026-05-16
 * Description:  Disables and re-enables SQL Server Agent jobs for testing.
 *               Useful for maintenance windows or troubleshooting.
 *               
 * Prerequisites: - Jobs created via CreateJobsForSource.sql
 *                
 * Usage:        1. Set @Action to 'DISABLE', 'ENABLE', or 'STATUS'
 *               2. Optionally filter by @JobNamePattern
 *               3. Execute script
 ******************************************************************************/

USE [DwBuilderDW];
GO

SET NOCOUNT ON;
GO

PRINT '========================================';
PRINT 'TEST SCENARIO 4: Disable/Enable Jobs';
PRINT 'Timestamp: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '========================================';
PRINT '';

-- Configuration
DECLARE @Action NVARCHAR(10) = 'STATUS'; -- OPTIONS: 'DISABLE', 'ENABLE', 'STATUS'
DECLARE @JobNamePattern NVARCHAR(200) = 'DwBuilder_%'; -- Pattern to match jobs

PRINT 'Action: ' + @Action;
PRINT 'Job pattern: ' + @JobNamePattern;
PRINT '';

-- Variables
DECLARE @JobName NVARCHAR(200);
DECLARE @Enabled INT;

IF @Action = 'STATUS'
BEGIN
    -- Display current status of matching jobs
    PRINT '--- Current Job Status ---';
    PRINT '';
    
    SELECT 
        name AS JobName,
        CASE enabled
            WHEN 1 THEN 'Enabled'
            ELSE 'Disabled'
        END AS Status,
        date_created AS CreatedDate,
        date_modified AS ModifiedDate
    FROM msdb.dbo.sysjobs
    WHERE name LIKE @JobNamePattern
    ORDER BY name;
    
    PRINT '';
    PRINT 'Status query completed.';
END
ELSE IF @Action = 'DISABLE'
BEGIN
    PRINT '--- Disabling Jobs ---';
    PRINT '';
    
    DECLARE disable_cursor CURSOR FOR
    SELECT name, enabled
    FROM msdb.dbo.sysjobs
    WHERE name LIKE @JobNamePattern
      AND enabled = 1; -- Only disable currently enabled jobs
    
    OPEN disable_cursor;
    FETCH NEXT FROM disable_cursor INTO @JobName, @Enabled;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT 'Disabling job: ' + @JobName;
        
        EXEC msdb.dbo.sp_update_job 
            @job_name = @JobName,
            @enabled = 0;
        
        FETCH NEXT FROM disable_cursor INTO @JobName, @Enabled;
    END
    
    CLOSE disable_cursor;
    DEALLOCATE disable_cursor;
    
    PRINT '';
    PRINT 'Jobs disabled successfully.';
    
    -- Verify
    PRINT '';
    PRINT 'Verification:';
    SELECT 
        name AS JobName,
        CASE enabled WHEN 1 THEN 'Enabled' ELSE 'Disabled' END AS Status
    FROM msdb.dbo.sysjobs
    WHERE name LIKE @JobNamePattern;
END
ELSE IF @Action = 'ENABLE'
BEGIN
    PRINT '--- Enabling Jobs ---';
    PRINT '';
    
    DECLARE enable_cursor CURSOR FOR
    SELECT name, enabled
    FROM msdb.dbo.sysjobs
    WHERE name LIKE @JobNamePattern
      AND enabled = 0; -- Only enable currently disabled jobs
    
    OPEN enable_cursor;
    FETCH NEXT FROM enable_cursor INTO @JobName, @Enabled;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT 'Enabling job: ' + @JobName;
        
        EXEC msdb.dbo.sp_update_job 
            @job_name = @JobName,
            @enabled = 1;
        
        FETCH NEXT FROM enable_cursor INTO @JobName, @Enabled;
    END
    
    CLOSE enable_cursor;
    DEALLOCATE enable_cursor;
    
    PRINT '';
    PRINT 'Jobs enabled successfully.';
    
    -- Verify
    PRINT '';
    PRINT 'Verification:';
    SELECT 
        name AS JobName,
        CASE enabled WHEN 1 THEN 'Enabled' ELSE 'Disabled' END AS Status
    FROM msdb.dbo.sysjobs
    WHERE name LIKE @JobNamePattern;
END
ELSE
BEGIN
    RAISERROR('Invalid action "%s". Valid options: DISABLE, ENABLE, STATUS', 16, 1, @Action);
END

PRINT '';
PRINT '--- Schedule Status ---';
PRINT '';

-- Display schedule status for matching jobs
SELECT 
    j.name AS JobName,
    s.name AS ScheduleName,
    CASE s.enabled
        WHEN 1 THEN 'Enabled'
        ELSE 'Disabled'
    END AS ScheduleStatus,
    CASE s.freq_type
        WHEN 4 THEN 'Daily'
        WHEN 8 THEN 'Weekly'
        WHEN 16 THEN 'Monthly'
        ELSE 'Other'
    END AS ScheduleType
FROM msdb.dbo.sysjobs j
INNER JOIN msdb.dbo.sysjobschedules js ON j.job_id = js.job_id
INNER JOIN msdb.dbo.sysschedules s ON js.schedule_id = s.schedule_id
WHERE j.name LIKE @JobNamePattern
ORDER BY j.name;

PRINT '';
PRINT '========================================';
PRINT 'Disable/Enable jobs test completed.';
PRINT '========================================';
GO
