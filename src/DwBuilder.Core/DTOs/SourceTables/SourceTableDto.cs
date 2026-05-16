namespace DwBuilder.Core.DTOs.SourceTables;

/// <summary>
/// DTO for configured source table.
/// </summary>
public record SourceTableDto
{
    public int Id { get; init; }
    
    public int SourceId { get; init; }
    
    public string SchemaName { get; init; } = null!;
    
    public string TableName { get; init; } = null!;
    
    public string LandingTableName { get; init; } = null!;
    
    public bool IsActive { get; init; }
    
    public DateTimeOffset? LastSyncAt { get; init; }
    
    public string? LastSyncStatus { get; init; }
    
    public DateTimeOffset CreatedAt { get; init; }
    
    public DateTimeOffset UpdatedAt { get; init; }
}
