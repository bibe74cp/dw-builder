using DwBuilder.Core.Entities;
using DwBuilder.Infrastructure.Data;
using DwBuilder.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DwBuilder.Tests.Infrastructure.Repositories;

/// <summary>
/// Unit tests for SourceRepository using InMemory database.
/// </summary>
public class SourceRepositoryTests : IDisposable
{
    private readonly DwBuilderDbContext _context;
    private readonly SourceRepository _repository;

    public SourceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DwBuilderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DwBuilderDbContext(options);
        _repository = new SourceRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddSourceToDatabase()
    {
        // Arrange
        var source = new Source
        {
            Name = "Test Source",
            ServerName = "localhost",
            DatabaseName = "TestDB",
            LandingSchema = "test_landing",
            IsActive = true
        };

        // Act
        var result = await _repository.AddAsync(source);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        var savedSource = await _context.Sources.FindAsync(result.Id);
        savedSource.Should().NotBeNull();
        savedSource!.Name.Should().Be("Test Source");
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnSource()
    {
        // Arrange
        var source = new Source
        {
            Name = "Test Source",
            ServerName = "localhost",
            DatabaseName = "TestDB",
            LandingSchema = "test_landing"
        };
        _context.Sources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(source.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(source.Id);
        result.Name.Should().Be("Test Source");
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllActiveSources()
    {
        // Arrange
        var sources = new[]
        {
            new Source { Name = "Source 1", ServerName = "server1", DatabaseName = "db1", LandingSchema = "s1", IsActive = true },
            new Source { Name = "Source 2", ServerName = "server2", DatabaseName = "db2", LandingSchema = "s2", IsActive = true },
            new Source { Name = "Source 3", ServerName = "server3", DatabaseName = "db3", LandingSchema = "s3", IsActive = false }
        };
        _context.Sources.AddRange(sources);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(s => s.Name == "Source 1");
        result.Should().Contain(s => s.Name == "Source 2");
        result.Should().Contain(s => s.Name == "Source 3");
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingSource()
    {
        // Arrange
        var source = new Source
        {
            Name = "Original Name",
            ServerName = "localhost",
            DatabaseName = "TestDB",
            LandingSchema = "test_landing"
        };
        _context.Sources.Add(source);
        await _context.SaveChangesAsync();

        // Act
        source.Name = "Updated Name";
        source.ServerName = "updated-server";
        await _repository.UpdateAsync(source);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Sources.FindAsync(source.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.ServerName.Should().Be("updated-server");
        updated.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSource()
    {
        // Arrange
        var source = new Source
        {
            Name = "To Delete",
            ServerName = "localhost",
            DatabaseName = "TestDB",
            LandingSchema = "test_landing"
        };
        _context.Sources.Add(source);
        await _context.SaveChangesAsync();
        var sourceId = source.Id;

        // Act
        await _repository.DeleteAsync(sourceId);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Sources.FindAsync(sourceId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithInclude_ShouldLoadNavigationProperties()
    {
        // Arrange
        var source = new Source
        {
            Name = "Source with Tables",
            ServerName = "localhost",
            DatabaseName = "TestDB",
            LandingSchema = "test_landing"
        };
        var table = new SourceTable
        {
            Source = source,
            SchemaName = "dbo",
            TableName = "TestTable",
            LandingTableName = "TestTable_L"
        };
        _context.Sources.Add(source);
        _context.SourceTables.Add(table);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(source.Id);
        await _context.Entry(result!).Collection(s => s.SourceTables).LoadAsync();

        // Assert
        result.Should().NotBeNull();
        result!.SourceTables.Should().HaveCount(1);
        result.SourceTables.First().TableName.Should().Be("TestTable");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
