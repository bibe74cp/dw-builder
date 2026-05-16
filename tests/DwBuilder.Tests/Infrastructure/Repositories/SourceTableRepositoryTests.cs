using DwBuilder.Core.Entities;
using DwBuilder.Infrastructure.Data;
using DwBuilder.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DwBuilder.Tests.Infrastructure.Repositories;

/// <summary>
/// Unit tests for SourceTableRepository using InMemory database.
/// </summary>
public class SourceTableRepositoryTests : IDisposable
{
    private readonly DwBuilderDbContext _context;
    private readonly SourceTableRepository _repository;

    public SourceTableRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DwBuilderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DwBuilderDbContext(options);
        _repository = new SourceTableRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddSourceTableToDatabase()
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

        var table = new SourceTable
        {
            SourceId = source.Id,
            SchemaName = "dbo",
            TableName = "Customers",
            LandingTableName = "Customers_L",
            IsActive = true
        };

        // Act
        var result = await _repository.AddAsync(table);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnTable()
    {
        // Arrange
        var source = new Source { Name = "Test", ServerName = "s", DatabaseName = "d", LandingSchema = "l" };
        var table = new SourceTable
        {
            Source = source,
            SchemaName = "dbo",
            TableName = "Orders",
            LandingTableName = "Orders_L"
        };
        _context.SourceTables.Add(table);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(table.Id);

        // Assert
        result.Should().NotBeNull();
        result!.TableName.Should().Be("Orders");
    }

    [Fact]
    public async Task GetBySourceIdAsync_ShouldReturnTablesForSource()
    {
        // Arrange
        var source1 = new Source { Name = "S1", ServerName = "s", DatabaseName = "d", LandingSchema = "l" };
        var source2 = new Source { Name = "S2", ServerName = "s", DatabaseName = "d", LandingSchema = "l" };
        _context.Sources.AddRange(source1, source2);
        await _context.SaveChangesAsync();

        var tables = new[]
        {
            new SourceTable { SourceId = source1.Id, SchemaName = "dbo", TableName = "T1", LandingTableName = "T1_L" },
            new SourceTable { SourceId = source1.Id, SchemaName = "dbo", TableName = "T2", LandingTableName = "T2_L" },
            new SourceTable { SourceId = source2.Id, SchemaName = "dbo", TableName = "T3", LandingTableName = "T3_L" }
        };
        _context.SourceTables.AddRange(tables);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetBySourceIdAsync(source1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.SourceId == source1.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyTable()
    {
        // Arrange
        var source = new Source { Name = "Test", ServerName = "s", DatabaseName = "d", LandingSchema = "l" };
        var table = new SourceTable
        {
            Source = source,
            SchemaName = "dbo",
            TableName = "Original",
            LandingTableName = "Original_L",
            IsActive = true
        };
        _context.SourceTables.Add(table);
        await _context.SaveChangesAsync();

        // Act
        table.IsActive = false;
        table.LastSyncStatus = "Failed";
        table.LastSyncMessage = "Error occurred";
        table.LastSyncAt = DateTimeOffset.UtcNow;
        await _repository.UpdateAsync(table);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.SourceTables.FindAsync(table.Id);
        updated.Should().NotBeNull();
        updated!.IsActive.Should().BeFalse();
        updated.LastSyncStatus.Should().Be("Failed");
        updated.LastSyncMessage.Should().Be("Error occurred");
        updated.LastSyncAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTable()
    {
        // Arrange
        var source = new Source { Name = "Test", ServerName = "s", DatabaseName = "d", LandingSchema = "l" };
        var table = new SourceTable
        {
            Source = source,
            SchemaName = "dbo",
            TableName = "ToDelete",
            LandingTableName = "ToDelete_L"
        };
        _context.SourceTables.Add(table);
        await _context.SaveChangesAsync();
        var tableId = table.Id;

        // Act
        await _repository.DeleteAsync(tableId);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.SourceTables.FindAsync(tableId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithInclude_ShouldLoadFields()
    {
        // Arrange
        var source = new Source { Name = "Test", ServerName = "s", DatabaseName = "d", LandingSchema = "l" };
        var table = new SourceTable
        {
            Source = source,
            SchemaName = "dbo",
            TableName = "TableWithFields",
            LandingTableName = "TableWithFields_L"
        };
        var field = new SourceField
        {
            SourceTable = table,
            SourceFieldName = "Id",
            LandingColumnName = "Id",
            DataType = "int",
            OrdinalPosition = 1
        };
        _context.SourceTables.Add(table);
        _context.SourceFields.Add(field);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(table.Id);
        await _context.Entry(result!).Collection(t => t.SourceFields).LoadAsync();

        // Assert
        result.Should().NotBeNull();
        result!.SourceFields.Should().HaveCount(1);
        result.SourceFields.First().SourceFieldName.Should().Be("Id");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
