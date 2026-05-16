using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DwBuilder.Infrastructure.Data;

public class DwBuilderDbContextFactory : IDesignTimeDbContextFactory<DwBuilderDbContext>
{
    public DwBuilderDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "DwBuilder.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<DwBuilderDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("DwBuilder"));

        return new DwBuilderDbContext(optionsBuilder.Options);
    }
}
