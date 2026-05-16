namespace DwBuilder.Core.Entities;

/// <summary>
/// Represents a log entry for Serilog sink to SQL Server.
/// </summary>
public class Log
{
    public long Id { get; set; }
    
    public DateTimeOffset Timestamp { get; set; }
    
    public string Level { get; set; } = null!;
    
    public string Message { get; set; } = null!;
    
    public string? Exception { get; set; }
    
    public string? Properties { get; set; }
}
