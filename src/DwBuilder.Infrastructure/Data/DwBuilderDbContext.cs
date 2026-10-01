using DwBuilder.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DwBuilder.Infrastructure.Data;

/// <summary>
/// Main database context for DW-Builder.
/// All entities are mapped to the _meta schema.
/// </summary>
public class DwBuilderDbContext : IdentityDbContext<IdentityUser>
{
    public DwBuilderDbContext(DbContextOptions<DwBuilderDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<SourceTable> SourceTables => Set<SourceTable>();
    public DbSet<SourceField> SourceFields => Set<SourceField>();
    public DbSet<Log> Logs => Set<Log>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure schema for all entities
        modelBuilder.HasDefaultSchema("_meta");
        
        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DwBuilderDbContext).Assembly);
        
        // Configure Identity tables to use _meta schema
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();
            if (tableName != null && tableName.StartsWith("AspNet"))
            {
                entityType.SetSchema("_meta");
            }
        }
    }
    
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }
    
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        
        var now = DateTimeOffset.UtcNow;
        
        foreach (var entry in entries)
        {
            // Check if the entity has CreatedAt and UpdatedAt properties
            var createdAtProperty = entry.Metadata.FindProperty("CreatedAt");
            var updatedAtProperty = entry.Metadata.FindProperty("UpdatedAt");
            
            if (entry.State == EntityState.Added && createdAtProperty != null)
            {
                var currentValue = entry.Property("CreatedAt").CurrentValue;
                if (currentValue == null || (DateTimeOffset)currentValue == default)
                {
                    entry.Property("CreatedAt").CurrentValue = now;
                }
            }
            
            if (updatedAtProperty != null)
            {
                entry.Property("UpdatedAt").CurrentValue = now;
            }
        }
    }
}
