namespace DwBuilder.Core.DTOs.SourceSchema;

/// <summary>
/// Represents a table available in the source database.
/// </summary>
public record SourceTableInfo
{
    public string SchemaName { get; init; } = null!;
    
    public string TableName { get; init; } = null!;
}
