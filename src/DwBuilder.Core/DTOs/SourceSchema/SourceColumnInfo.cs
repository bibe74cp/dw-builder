namespace DwBuilder.Core.DTOs.SourceSchema;

/// <summary>
/// Represents a column available in a source table.
/// </summary>
public record SourceColumnInfo
{
    public string ColumnName { get; init; } = null!;
    
    public string DataType { get; init; } = null!;
    
    public bool IsNullable { get; init; }
    
    public int OrdinalPosition { get; init; }
    
    public int? MaxLength { get; init; }
    
    public int? Precision { get; init; }
    
    public int? Scale { get; init; }
}
