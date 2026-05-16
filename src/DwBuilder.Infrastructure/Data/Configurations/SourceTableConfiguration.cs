using DwBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DwBuilder.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for SourceTable entity.
/// </summary>
public class SourceTableConfiguration : IEntityTypeConfiguration<SourceTable>
{
    public void Configure(EntityTypeBuilder<SourceTable> builder)
    {
        builder.ToTable("SourceTables", "_meta");
        
        builder.HasKey(st => st.Id);
        
        builder.Property(st => st.Id)
            .UseIdentityColumn();
        
        builder.Property(st => st.SourceId)
            .IsRequired();
        
        builder.Property(st => st.SchemaName)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(st => st.TableName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(st => st.LandingTableName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(st => st.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        builder.Property(st => st.LastSyncAt);
        
        builder.Property(st => st.LastSyncStatus)
            .HasMaxLength(50);
        
        builder.Property(st => st.LastSyncMessage)
            .HasMaxLength(4000);
        
        builder.Property(st => st.CreatedAt)
            .IsRequired();
        
        builder.Property(st => st.UpdatedAt)
            .IsRequired();
        
        // Relationships
        builder.HasOne(st => st.Source)
            .WithMany(s => s.SourceTables)
            .HasForeignKey(st => st.SourceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(st => st.SourceFields)
            .WithOne(sf => sf.SourceTable)
            .HasForeignKey(sf => sf.SourceTableId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(st => new { st.SourceId, st.SchemaName, st.TableName })
            .IsUnique();
        
        builder.HasIndex(st => st.IsActive);
    }
}
