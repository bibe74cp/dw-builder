using DwBuilder.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        
        // Seed development data
        SeedDevelopmentData(services).GetAwaiter().GetResult();
        
        return host;
    }
    
    /// <summary>
    /// Seeds development data including default users.
    /// </summary>
    private static async Task SeedDevelopmentData(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var logger = services.GetRequiredService<ILogger<DwBuilderDbContext>>();
        
        // Create development admin user
        const string devAdminEmail = "dev_admin@dwbuilder.local";
        const string devAdminUsername = "dev_admin";
        const string devAdminPassword = "SuchAStrongPassw0rd!";
        
        var existingUser = await userManager.FindByNameAsync(devAdminUsername);
        if (existingUser == null)
        {
            var devAdmin = new IdentityUser
            {
                UserName = devAdminUsername,
                Email = devAdminEmail,
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(devAdmin, devAdminPassword);
            
            if (result.Succeeded)
            {
                logger.LogInformation("Development admin user '{Username}' created successfully", devAdminUsername);
            }
            else
            {
                logger.LogWarning("Failed to create development admin user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("Development admin user '{Username}' already exists", devAdminUsername);
        }
    }
}
