using System.ComponentModel.DataAnnotations;

namespace DwBuilder.Core.DTOs.SourceTables;

/// <summary>
/// Request for bulk updating source table configurations.
/// </summary>
public record BulkUpdateTablesRequest
{
    [Required]
    public List<TableConfigItem> Tables { get; init; } = new();
}

/// <summary>
/// Single table configuration item.
/// </summary>
public record TableConfigItem
{
    [Required]
    public string SchemaName { get; init; } = null!;
    
    [Required]
    public string TableName { get; init; } = null!;
    
    [Required]
    [RegularExpression(@"^[A-Za-z_][A-Za-z0-9_]*$", ErrorMessage = "LandingTableName must be a valid SQL identifier")]
    public string LandingTableName { get; init; } = null!;
    
    public bool IsActive { get; init; } = true;
}
