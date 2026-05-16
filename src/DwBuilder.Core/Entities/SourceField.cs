namespace DwBuilder.Core.Entities;

/// <summary>
/// Represents a field/column mapping from a source table to the landing zone.
/// </summary>
public class SourceField
{
    public int Id { get; set; }
    
    public int SourceTableId { get; set; }
    
    public string SourceColumnName { get; set; } = null!;
    
    public string LandingColumnName { get; set; } = null!;
    
    public string SqlDataType { get; set; } = null!;
    
    public bool IsBusinessKey { get; set; } = false;
    
    public bool IsNullable { get; set; } = true;
    
    public int OrdinalPosition { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Navigation properties
    public SourceTable SourceTable { get; set; } = null!;
}
