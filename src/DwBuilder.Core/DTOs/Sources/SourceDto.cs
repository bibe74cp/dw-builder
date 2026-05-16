namespace DwBuilder.Core.DTOs.Sources;

/// <summary>
/// DTO for returning source information to the client (password excluded).
/// </summary>
public class SourceDto
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string ServerName { get; set; } = null!;
    
    public string? InstanceName { get; set; }
    
    public string DatabaseName { get; set; } = null!;
    
    public string LandingSchema { get; set; } = null!;
    
    public string? ConnectionUser { get; set; }

    public bool HasPassword { get; set; }

    public bool IsActive { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset UpdatedAt { get; set; }
}
