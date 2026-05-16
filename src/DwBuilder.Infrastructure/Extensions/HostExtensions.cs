using DwBuilder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DwBuilder.Infrastructure.Extensions;

/// <summary>
/// Extension methods for IHost to apply EF Core migrations at startup.
/// </summary>
public static class HostExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations to the database.
    /// Should be called after app.Build() in Program.cs.
    /// </summary>
    public static IHost MigrateDatabase(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        var context = services.GetRequiredService<DwBuilderDbContext>();
        context.Database.Migrate();
        
        return host;
    }
}
