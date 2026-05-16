/*******************************************************************************
 * File:         05_DeleteJobs.sql
 * Author:       db-developer
 * Date:         2026-05-16
 * Description:  Deletes SQL Server Agent jobs created for testing purposes.
 *               USE WITH CAUTION - this permanently removes jobs.
 *               
 * Prerequisites: - Jobs created via CreateJobsForSource.sql
 *                
 * Usage:        1. Review jobs to be deleted by setting @DryRun = 1
 *               2. Set @DryRun = 0 to actually delete jobs
 *               3. Execute script
 ******************************************************************************/

USE [DwBuilderDW];
GO

SET NOCOUNT ON;
GO

PRINT '========================================';
PRINT 'TEST SCENARIO 5: Delete Test Jobs';
PRINT 'Timestamp: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
PRINT '========================================';
PRINT '';

-- Configuration
DECLARE @DryRun BIT = 1; -- Set to 0 to actually delete jobs
DECLARE @JobNamePattern NVARCHAR(200) = 'DwBuilder_Source1_%'; -- Pattern to match test jobs

PRINT 'Dry Run Mode: ' + CASE WHEN @DryRun = 1 THEN 'YES (no changes will be made)' ELSE 'NO (JOBS WILL BE DELETED)' END;
PRINT 'Job pattern: ' + @JobNamePattern;
PRINT '';

-- Display jobs to be deleted
PRINT '--- Jobs matching pattern ---';
PRINT '';

SELECT 
    j.name AS JobName,
    CASE j.enabled WHEN 1 THEN 'Enabled' ELSE 'Disabled' END AS Status,
    j.date_created AS CreatedDate,
    COUNT(DISTINCT js.schedule_id) AS ScheduleCount
FROM msdb.dbo.sysjobs j
LEFT JOIN msdb.dbo.sysjobschedules js ON j.job_id = js.job_id
WHERE j.name LIKE @JobNamePattern
GROUP BY j.name, j.enabled, j.date_created
ORDER BY j.name;

-- Count
DECLARE @JobCount INT;
SELECT @JobCount = COUNT(*)
FROM msdb.dbo.sysjobs
WHERE name LIKE @JobNamePattern;

PRINT '';
PRINT 'Total jobs to delete: ' + CAST(@JobCount AS NVARCHAR(10));
PRINT '';

IF @JobCount = 0
BEGIN
    PRINT 'No jobs found matching pattern. Nothing to delete.';
    RETURN;
END

IF @DryRun = 1
BEGIN
    PRINT '*** DRY RUN MODE ***';
    PRINT 'Set @DryRun = 0 in the script to actually delete these jobs.';
    PRINT '';
    RETURN;
END

-- Confirm deletion (manual safety check)
PRINT '!!! WARNING !!!';
PRINT 'About to delete ' + CAST(@JobCount AS NVARCHAR(10)) + ' job(s).';
PRINT 'This action cannot be undone.';
PRINT '';
PRINT 'Proceeding with deletion in 5 seconds... (Cancel script execution now if this is not intended)';
WAITFOR DELAY '00:00:05';
PRINT '';

-- Delete jobs
PRINT '--- Deleting Jobs ---';
PRINT '';

DECLARE @JobName NVARCHAR(200);

DECLARE delete_cursor CURSOR FOR
SELECT name
FROM msdb.dbo.sysjobs
WHERE name LIKE @JobNamePattern
ORDER BY name;

OPEN delete_cursor;
FETCH NEXT FROM delete_cursor INTO @JobName;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Deleting job: ' + @JobName;
    
    BEGIN TRY
        EXEC msdb.dbo.sp_delete_job 
            @job_name = @JobName,
            @delete_history = 1, -- Also delete job history
            @delete_unused_schedule = 1; -- Delete schedules not used by other jobs
        
        PRINT '  - Deleted successfully.';
    END TRY
    BEGIN CATCH
        PRINT '  - ERROR: ' + ERROR_MESSAGE();
    END CATCH
    
    FETCH NEXT FROM delete_cursor INTO @JobName;
END

CLOSE delete_cursor;
DEALLOCATE delete_cursor;

PRINT '';
PRINT '--- Deletion Summary ---';
PRINT '';

-- Verify deletion
DECLARE @RemainingJobs INT;
SELECT @RemainingJobs = COUNT(*)
FROM msdb.dbo.sysjobs
WHERE name LIKE @JobNamePattern;

PRINT 'Remaining jobs matching pattern: ' + CAST(@RemainingJobs AS NVARCHAR(10));

IF @RemainingJobs > 0
BEGIN
    PRINT '';
    PRINT 'WARNING: Some jobs were not deleted. Review errors above.';
    PRINT '';
    
    SELECT name AS RemainingJobs
    FROM msdb.dbo.sysjobs
    WHERE name LIKE @JobNamePattern;
END
ELSE
BEGIN
    PRINT 'All jobs deleted successfully.';
END

PRINT '';
PRINT '--- Cleanup _meta.Logs entries (optional) ---';
PRINT '';
PRINT 'Job execution logs remain in _meta.Logs for historical purposes.';
PRINT 'To delete them, execute:';
PRINT '';
PRINT '  DELETE FROM [_meta].[Logs] WHERE JobName LIKE ''' + @JobNamePattern + ''';';
PRINT '';

PRINT '========================================';
PRINT 'Job deletion test completed.';
PRINT '========================================';
GO
