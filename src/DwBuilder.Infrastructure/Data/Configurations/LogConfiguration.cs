using DwBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DwBuilder.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Log entity (Serilog sink).
/// </summary>
public class LogConfiguration : IEntityTypeConfiguration<Log>
{
    public void Configure(EntityTypeBuilder<Log> builder)
    {
        builder.ToTable("Logs", "_meta");
        
        builder.HasKey(l => l.Id);
        
        builder.Property(l => l.Id)
            .UseIdentityColumn();
        
        builder.Property(l => l.Timestamp)
            .IsRequired();
        
        builder.Property(l => l.Level)
            .IsRequired()
            .HasMaxLength(15);
        
        builder.Property(l => l.Message)
            .IsRequired();
        
        builder.Property(l => l.Exception);
        
        builder.Property(l => l.Properties);
        
        // Indexes
        builder.HasIndex(l => l.Timestamp);
        
        builder.HasIndex(l => l.Level);
    }
}
