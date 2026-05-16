namespace DwBuilder.Core.Entities;

/// <summary>
/// Represents a log entry for Serilog sink to SQL Server.
/// Enhanced with SQL Server Agent job execution tracking (FASE 7).
/// </summary>
public class Log
{
    public long Id { get; set; }
    
    public DateTimeOffset Timestamp { get; set; }
    
    public string Level { get; set; } = null!;
    
    public string Message { get; set; } = null!;
    
    public string? Exception { get; set; }
    
    public string? Properties { get; set; }
    
    // SQL Server Agent job tracking (FASE 7)
    public string? JobName { get; set; }
    
    public Guid? JobExecutionId { get; set; }
    
    public string? PackageName { get; set; }
    
    public int? RowsInserted { get; set; }
    
    public int? RowsUpdated { get; set; }
    
    public int? RowsDeleted { get; set; }
    
    public int? ExecutionDurationMs { get; set; }
    
    public string? ErrorDetails { get; set; }
}
