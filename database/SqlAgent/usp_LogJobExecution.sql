/*******************************************************************************
 * Stored Procedure: [_meta].[usp_LogJobExecution]
 * Author:          db-developer
 * Date:            2026-05-16
 * Description:     Logs SQL Server Agent job execution details to _meta.Logs.
 *                  Called from SQL Agent job steps after SSIS package execution.
 *                  
 * Parameters:      @JobName - Name of the SQL Server Agent job
 *                  @PackageName - Name of the SSIS package executed
 *                  @Status - Execution status ('Success', 'Error', 'Warning')
 *                  @RowsInserted - Number of rows inserted (nullable)
 *                  @RowsUpdated - Number of rows updated (nullable)
 *                  @RowsDeleted - Number of rows deleted (nullable)
 *                  @DurationMs - Execution duration in milliseconds
 *                  @ErrorDetails - Error message if failed (nullable)
 *                  
 * Returns:         Log entry ID
 * 
 * Example Usage:   
 *   EXEC [_meta].[usp_LogJobExecution]
 *       @JobName = N'DwBuilder_Source1_Table5_Customers',
 *       @PackageName = N'Customers.dtsx',
 *       @Status = 'Success',
 *       @RowsInserted = 150,
 *       @RowsUpdated = 25,
 *       @RowsDeleted = 3,
 *       @DurationMs = 12350,
 *       @ErrorDetails = NULL;
 ******************************************************************************/

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Drop if exists
IF OBJECT_ID('[_meta].[usp_LogJobExecution]', 'P') IS NOT NULL
    DROP PROCEDURE [_meta].[usp_LogJobExecution];
GO

CREATE PROCEDURE [_meta].[usp_LogJobExecution]
    @JobName NVARCHAR(200),
    @PackageName NVARCHAR(200),
    @Status NVARCHAR(50),
    @RowsInserted INT = NULL,
    @RowsUpdated INT = NULL,
    @RowsDeleted INT = NULL,
    @DurationMs INT = NULL,
    @ErrorDetails NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @LogLevel NVARCHAR(15);
    DECLARE @Message NVARCHAR(MAX);
    DECLARE @JobExecutionId UNIQUEIDENTIFIER;
    
    -- Generate execution ID for correlation
    SET @JobExecutionId = NEWID();
    
    -- Determine log level based on status
    SET @LogLevel = CASE @Status
        WHEN 'Success' THEN 'Information'
        WHEN 'Warning' THEN 'Warning'
        WHEN 'Error' THEN 'Error'
        ELSE 'Information'
    END;
    
    -- Build log message
    SET @Message = N'SQL Agent Job Execution: ' + @JobName + N' | Package: ' + @PackageName + N' | Status: ' + @Status;
    
    IF @RowsInserted IS NOT NULL OR @RowsUpdated IS NOT NULL OR @RowsDeleted IS NOT NULL
    BEGIN
        SET @Message = @Message + N' | Rows: Inserted=' + ISNULL(CAST(@RowsInserted AS NVARCHAR(10)), 'N/A') 
                                 + N', Updated=' + ISNULL(CAST(@RowsUpdated AS NVARCHAR(10)), 'N/A')
                                 + N', Deleted=' + ISNULL(CAST(@RowsDeleted AS NVARCHAR(10)), 'N/A');
    END
    
    IF @DurationMs IS NOT NULL
    BEGIN
        SET @Message = @Message + N' | Duration: ' + CAST(@DurationMs AS NVARCHAR(10)) + N' ms';
    END
    
    -- Insert log entry
    INSERT INTO [_meta].[Logs] (
        [Timestamp],
        [Level],
        [Message],
        [Exception],
        [Properties],
        [JobName],
        [JobExecutionId],
        [PackageName],
        [RowsInserted],
        [RowsUpdated],
        [RowsDeleted],
        [ExecutionDurationMs],
        [ErrorDetails]
    )
    VALUES (
        SYSDATETIMEOFFSET(),
        @LogLevel,
        @Message,
        CASE WHEN @Status = 'Error' THEN @ErrorDetails ELSE NULL END,
        NULL, -- Properties (JSON) can be extended if needed
        @JobName,
        @JobExecutionId,
        @PackageName,
        @RowsInserted,
        @RowsUpdated,
        @RowsDeleted,
        @DurationMs,
        @ErrorDetails
    );
    
    -- Return the log entry ID
    SELECT SCOPE_IDENTITY() AS LogId;
    
    RETURN 0;
END
GO

-- Grant execute permission to appropriate roles
-- GRANT EXECUTE ON [_meta].[usp_LogJobExecution] TO [DwBuilderRole];
-- GO
