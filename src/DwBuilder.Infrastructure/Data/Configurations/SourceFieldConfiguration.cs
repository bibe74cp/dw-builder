using DwBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DwBuilder.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for SourceField entity.
/// </summary>
public class SourceFieldConfiguration : IEntityTypeConfiguration<SourceField>
{
    public void Configure(EntityTypeBuilder<SourceField> builder)
    {
        builder.ToTable("SourceFields", "_meta");
        
        builder.HasKey(sf => sf.Id);
        
        builder.Property(sf => sf.Id)
            .UseIdentityColumn();
        
        builder.Property(sf => sf.SourceTableId)
            .IsRequired();
        
        builder.Property(sf => sf.SourceColumnName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(sf => sf.LandingColumnName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(sf => sf.SqlDataType)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(sf => sf.IsBusinessKey)
            .IsRequired()
            .HasDefaultValue(false);
        
        builder.Property(sf => sf.IsNullable)
            .IsRequired()
            .HasDefaultValue(true);
        
        builder.Property(sf => sf.OrdinalPosition)
            .IsRequired();
        
        builder.Property(sf => sf.CreatedAt)
            .IsRequired();
        
        builder.Property(sf => sf.UpdatedAt)
            .IsRequired();
        
        // Relationships
        builder.HasOne(sf => sf.SourceTable)
            .WithMany(st => st.SourceFields)
            .HasForeignKey(sf => sf.SourceTableId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(sf => new { sf.SourceTableId, sf.SourceColumnName })
            .IsUnique();
        
        builder.HasIndex(sf => sf.IsBusinessKey);
    }
}
