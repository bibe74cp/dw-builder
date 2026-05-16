using DwBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DwBuilder.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Source entity.
/// </summary>
public class SourceConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        builder.ToTable("Sources", "_meta");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Id)
            .UseIdentityColumn();
        
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(s => s.ServerName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(s => s.InstanceName)
            .HasMaxLength(100);
        
        builder.Property(s => s.DatabaseName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(s => s.LandingSchema)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(s => s.ConnectionUser)
            .HasMaxLength(200);
        
        builder.Property(s => s.ConnectionPasswordEncrypted)
            .HasMaxLength(500);
        
        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        builder.Property(s => s.CreatedAt)
            .IsRequired();
        
        builder.Property(s => s.UpdatedAt)
            .IsRequired();
        
        // Relationships
        builder.HasMany(s => s.SourceTables)
            .WithOne(st => st.Source)
            .HasForeignKey(st => st.SourceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(s => s.Name)
            .IsUnique();
        
        builder.HasIndex(s => s.IsActive);
    }
}
