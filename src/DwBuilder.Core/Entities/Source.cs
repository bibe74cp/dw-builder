namespace DwBuilder.Core.Entities;

/// <summary>
/// Represents a source database system from which data is extracted.
/// </summary>
public class Source
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string ServerName { get; set; } = null!;
    
    public string? InstanceName { get; set; }
    
    public string DatabaseName { get; set; } = null!;
    
    public string LandingSchema { get; set; } = null!;
    
    public string? ConnectionUser { get; set; }
    
    public string? ConnectionPasswordEncrypted { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<SourceTable> SourceTables { get; set; } = new List<SourceTable>();
}
