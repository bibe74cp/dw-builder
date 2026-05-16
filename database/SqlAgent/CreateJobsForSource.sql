/*******************************************************************************
 * File:         CreateJobsForSource.sql
 * Author:       db-developer
 * Date:         2026-05-16
 * Description:  Creates SQL Server Agent jobs for all enabled SourceTables 
 *               in a specified Source, with parameterized scheduling.
 *               
 * Usage:        EXEC [database\SqlAgent\CreateJobsForSource.sql]
 *               with parameters @SourceId and @SsisCatalogFolder
 *               
 * Dependencies: - SQL Server Agent must be running
 *               - SSIS Catalog (SSISDB) must be configured
 *               - Packages must be deployed to SSISDB/DwBuilder/{SourceName}/
 *               - Caller must have ssis_admin or sysadmin role
 *               
 * Notes:        - Idempotent: drops existing jobs before recreating
 *               - Job naming: DwBuilder_Source{SourceId}_Table{TableId}_{TableName}
 *               - Schedule based on ScheduleType, Frequency, Time, DaysOfWeek
 ******************************************************************************/

SET NOCOUNT ON;
GO

DECLARE @SourceId INT = 1; -- Parameter: ID of the source to create jobs for
DECLARE @SsisCatalogFolder NVARCHAR(200) = N'DwBuilder'; -- SSISDB folder name

-- Variables
DECLARE @SourceName NVARCHAR(100);
DECLARE @SourceTableId INT;
DECLARE @TableName NVARCHAR(200);
DECLARE @LandingSchema NVARCHAR(100);
DECLARE @JobName NVARCHAR(200);
DECLARE @PackageName NVARCHAR(200);
DECLARE @PackagePath NVARCHAR(500);
DECLARE @ScheduleEnabled BIT;
DECLARE @ScheduleType NVARCHAR(20);
DECLARE @ScheduleFrequency INT;
DECLARE @ScheduleTime TIME;
DECLARE @ScheduleDaysOfWeek NVARCHAR(50);
DECLARE @ScheduleName NVARCHAR(200);
DECLARE @StepCommand NVARCHAR(MAX);
DECLARE @FreqType INT;
DECLARE @FreqInterval INT;
DECLARE @FreqSubdayType INT;
DECLARE @FreqSubdayInterval INT;
DECLARE @ActiveStartTime INT;

-- Get source details
SELECT 
    @SourceName = Name,
    @LandingSchema = LandingSchema
FROM [_meta].[Sources]
WHERE Id = @SourceId;

IF @SourceName IS NULL
BEGIN
    RAISERROR('Source with ID %d not found.', 16, 1, @SourceId);
    RETURN;
END

PRINT '========================================';
PRINT 'Creating SQL Server Agent Jobs';
PRINT 'Source: ' + @SourceName + ' (ID: ' + CAST(@SourceId AS NVARCHAR(10)) + ')';
PRINT 'SSIS Catalog Folder: ' + @SsisCatalogFolder;
PRINT '========================================';
PRINT '';

-- Cursor to iterate over enabled SourceTables
DECLARE table_cursor CURSOR FOR
SELECT 
    Id,
    TableName,
    ScheduleEnabled,
    ScheduleType,
    ScheduleFrequency,
    ScheduleTime,
    ScheduleDaysOfWeek
FROM [_meta].[SourceTables]
WHERE SourceId = @SourceId 
  AND IsActive = 1
  AND ScheduleEnabled = 1
ORDER BY TableName;

OPEN table_cursor;

FETCH NEXT FROM table_cursor INTO 
    @SourceTableId, @TableName, @ScheduleEnabled, @ScheduleType, 
    @ScheduleFrequency, @ScheduleTime, @ScheduleDaysOfWeek;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Construct job and package names
    SET @JobName = N'DwBuilder_Source' + CAST(@SourceId AS NVARCHAR(10)) 
                   + N'_Table' + CAST(@SourceTableId AS NVARCHAR(10)) 
                   + N'_' + @TableName;
    
    SET @PackageName = @TableName + N'.dtsx';
    SET @PackagePath = N'/' + @SsisCatalogFolder + N'/' + @SourceName + N'/' + @PackageName;
    SET @ScheduleName = @JobName + N'_Schedule';
    
    PRINT 'Processing: ' + @JobName;
    
    -- Drop job if exists (idempotent)
    IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = @JobName)
    BEGIN
        PRINT '  - Dropping existing job...';
        EXEC msdb.dbo.sp_delete_job @job_name = @JobName;
    END
    
    -- Create job
    PRINT '  - Creating job...';
    EXEC msdb.dbo.sp_add_job 
        @job_name = @JobName,
        @enabled = 1,
        @description = N'DW-Builder ETL job for table ' + @LandingSchema + N'.' + @TableName,
        @category_name = N'Data Collector',
        @owner_login_name = N'sa';
    
    -- Create job step: execute SSIS package from SSISDB Catalog
    SET @StepCommand = N'
DECLARE @execution_id BIGINT;

EXEC [SSISDB].[catalog].[create_execution]
    @package_name = N''' + @PackageName + N''',
    @folder_name = N''' + @SsisCatalogFolder + N''',
    @project_name = N''' + @SourceName + N''',
    @use32bitruntime = 0,
    @execution_id = @execution_id OUTPUT;

EXEC [SSISDB].[catalog].[start_execution] @execution_id;

-- Wait for completion and log results
DECLARE @status INT, @start_time DATETIME2, @end_time DATETIME2, @duration_ms INT;

WHILE 1=1
BEGIN
    SELECT @status = [status], @start_time = start_time, @end_time = end_time
    FROM [SSISDB].[catalog].[executions]
    WHERE execution_id = @execution_id;
    
    IF @status IN (4, 7) -- 4=succeeded, 7=stopped
        BREAK;
    ELSE IF @status IN (5, 6) -- 5=ended_unexpectedly, 6=cancelled
    BEGIN
        RAISERROR(''Package execution failed. Check SSISDB.catalog.executions for details.'', 16, 1);
        RETURN;
    END
    
    WAITFOR DELAY ''00:00:05''; -- Poll every 5 seconds
END

SET @duration_ms = DATEDIFF(MILLISECOND, @start_time, @end_time);

-- Log to _meta.Logs via stored procedure
EXEC [_meta].[usp_LogJobExecution]
    @JobName = N''' + @JobName + N''',
    @PackageName = N''' + @PackageName + N''',
    @Status = ''Success'',
    @RowsInserted = NULL, -- Will be populated by SSIS package if available
    @RowsUpdated = NULL,
    @RowsDeleted = NULL,
    @DurationMs = @duration_ms,
    @ErrorDetails = NULL;
';
    
    PRINT '  - Creating job step...';
    EXEC msdb.dbo.sp_add_jobstep 
        @job_name = @JobName,
        @step_name = N'Execute SSIS Package',
        @subsystem = N'TSQL',
        @command = @StepCommand,
        @database_name = N'DwBuilderDW',
        @on_success_action = 1, -- Quit with success
        @on_fail_action = 2; -- Quit with failure
    
    -- Add job to local server
    EXEC msdb.dbo.sp_add_jobserver 
        @job_name = @JobName,
        @server_name = N'(LOCAL)';
    
    -- Create schedule based on ScheduleType
    IF @ScheduleType IS NOT NULL
    BEGIN
        PRINT '  - Creating schedule (' + @ScheduleType + ')...';
        
        -- Parse schedule parameters
        SET @ActiveStartTime = CASE 
            WHEN @ScheduleTime IS NOT NULL THEN 
                DATEPART(HOUR, @ScheduleTime) * 10000 + 
                DATEPART(MINUTE, @ScheduleTime) * 100 + 
                DATEPART(SECOND, @ScheduleTime)
            ELSE 0 -- Midnight if not specified
        END;
        
        IF @ScheduleType = 'Daily'
        BEGIN
            SET @FreqType = 4; -- Daily
            SET @FreqInterval = ISNULL(@ScheduleFrequency, 1); -- Every N days
            SET @FreqSubdayType = 1; -- At specified time
            SET @FreqSubdayInterval = 0;
        END
        ELSE IF @ScheduleType = 'Weekly'
        BEGIN
            SET @FreqType = 8; -- Weekly
            -- Convert DaysOfWeek string to bitmap (1=Sunday, 2=Monday, 4=Tuesday, etc.)
            SET @FreqInterval = 0;
            IF CHARINDEX('Sunday', @ScheduleDaysOfWeek) > 0 SET @FreqInterval = @FreqInterval + 1;
            IF CHARINDEX('Monday', @ScheduleDaysOfWeek) > 0 SET @FreqInterval = @FreqInterval + 2;
            IF CHARINDEX('Tuesday', @ScheduleDaysOfWeek) > 0 SET @FreqInterval = @FreqInterval + 4;
            IF CHARINDEX('Wednesday', @ScheduleDaysOfWeek) > 0 SET @FreqInterval = @FreqInterval + 8;
            IF CHARINDEX('Thursday', @ScheduleDaysOfWeek) > 0 SET @FreqInterval = @FreqInterval + 16;
            IF CHARINDEX('Friday', @ScheduleDaysOfWeek) > 0 SET @FreqInterval = @FreqInterval + 32;
            IF CHARINDEX('Saturday', @ScheduleDaysOfWeek) > 0 SET @FreqInterval = @FreqInterval + 64;
            IF @FreqInterval = 0 SET @FreqInterval = 2; -- Default to Monday if not specified
            SET @FreqSubdayType = 1;
            SET @FreqSubdayInterval = 0;
        END
        ELSE IF @ScheduleType = 'Monthly'
        BEGIN
            SET @FreqType = 16; -- Monthly on specified day
            SET @FreqInterval = ISNULL(@ScheduleFrequency, 1); -- Day of month
            SET @FreqSubdayType = 1;
            SET @FreqSubdayInterval = 0;
        END
        ELSE IF @ScheduleType = 'OnDemand'
        BEGIN
            -- No schedule, manual execution only
            PRINT '  - Schedule type OnDemand: no automatic schedule created.';
            GOTO SkipSchedule;
        END
        
        EXEC msdb.dbo.sp_add_jobschedule
            @job_name = @JobName,
            @name = @ScheduleName,
            @enabled = 1,
            @freq_type = @FreqType,
            @freq_interval = @FreqInterval,
            @freq_subday_type = @FreqSubdayType,
            @freq_subday_interval = @FreqSubdayInterval,
            @active_start_time = @ActiveStartTime;
        
        SkipSchedule:
    END
    ELSE
    BEGIN
        PRINT '  - No schedule type specified: job will not run automatically.';
    END
    
    PRINT '  - Job created successfully.';
    PRINT '';
    
    FETCH NEXT FROM table_cursor INTO 
        @SourceTableId, @TableName, @ScheduleEnabled, @ScheduleType, 
        @ScheduleFrequency, @ScheduleTime, @ScheduleDaysOfWeek;
END

CLOSE table_cursor;
DEALLOCATE table_cursor;

PRINT '========================================';
PRINT 'Job creation completed.';
PRINT '========================================';
GO
