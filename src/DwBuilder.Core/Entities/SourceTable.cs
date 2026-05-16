namespace DwBuilder.Core.Entities;

/// <summary>
/// Represents a table from a source database that is configured for ETL.
/// </summary>
public class SourceTable
{
    public int Id { get; set; }
    
    public int SourceId { get; set; }
    
    public string SchemaName { get; set; } = null!;
    
    public string TableName { get; set; } = null!;
    
    public string LandingTableName { get; set; } = null!;
    
    public bool IsActive { get; set; } = true;
    
    public DateTimeOffset? LastSyncAt { get; set; }
    
    public string? LastSyncStatus { get; set; }
    
    public string? LastSyncMessage { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Scheduling configuration (FASE 7)
    public bool ScheduleEnabled { get; set; } = false;
    
    public string? ScheduleType { get; set; }
    
    public int? ScheduleFrequency { get; set; }
    
    public TimeSpan? ScheduleTime { get; set; }
    
    public string? ScheduleDaysOfWeek { get; set; }
    
    public string? ScheduleDescription { get; set; }
    
    // Navigation properties
    public Source Source { get; set; } = null!;
    
    public ICollection<SourceField> SourceFields { get; set; } = new List<SourceField>();
}
