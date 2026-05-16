using System.ComponentModel.DataAnnotations;

namespace DwBuilder.Core.DTOs.Sources;

/// <summary>
/// Request DTO for updating an existing source.
/// </summary>
public class UpdateSourceRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;
    
    [Required]
    [MaxLength(200)]
    public string ServerName { get; set; } = null!;
    
    [MaxLength(100)]
    public string? InstanceName { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string DatabaseName { get; set; } = null!;
    
    [Required]
    [MaxLength(100)]
    [RegularExpression(@"^[A-Za-z_][A-Za-z0-9_]*$", ErrorMessage = "LandingSchema must be a valid SQL identifier")]
    public string LandingSchema { get; set; } = null!;
    
    [MaxLength(200)]
    public string? ConnectionUser { get; set; }
    
    /// <summary>
    /// If provided (not null or empty), the password will be re-encrypted.
    /// If null or empty, the existing encrypted password is preserved.
    /// </summary>
    [MaxLength(500)]
    public string? ConnectionPassword { get; set; }
}
