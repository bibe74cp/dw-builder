using System.ComponentModel.DataAnnotations;

namespace DwBuilder.Core.DTOs.SourceFields;

/// <summary>
/// Request for bulk updating source field configurations.
/// </summary>
public record BulkUpdateFieldsRequest
{
    [Required]
    public List<FieldConfigItem> Fields { get; init; } = new();
}

/// <summary>
/// Single field configuration item.
/// </summary>
public record FieldConfigItem
{
    [Required]
    public string SourceColumnName { get; init; } = null!;
    
    [Required]
    [RegularExpression(@"^[A-Za-z_][A-Za-z0-9_]*$", ErrorMessage = "LandingColumnName must be a valid SQL identifier")]
    public string LandingColumnName { get; init; } = null!;
    
    [Required]
    public string SqlDataType { get; init; } = null!;
    
    public bool IsBusinessKey { get; init; }
    
    public bool IsNullable { get; init; } = true;
    
    [Range(1, int.MaxValue)]
    public int OrdinalPosition { get; init; }
}
