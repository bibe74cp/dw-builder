namespace DwBuilder.Core.DTOs.SourceFields;

/// <summary>
/// DTO for configured source field.
/// </summary>
public record SourceFieldDto
{
    public int Id { get; init; }
    
    public int SourceTableId { get; init; }
    
    public string SourceColumnName { get; init; } = null!;
    
    public string LandingColumnName { get; init; } = null!;
    
    public string SqlDataType { get; init; } = null!;
    
    public bool IsBusinessKey { get; init; }
    
    public bool IsNullable { get; init; }
    
    public int OrdinalPosition { get; init; }
    
    public DateTimeOffset CreatedAt { get; init; }
    
    public DateTimeOffset UpdatedAt { get; init; }
}
