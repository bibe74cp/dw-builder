using DwBuilder.Core.Entities;
using DwBuilder.Infrastructure.Data;
using DwBuilder.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DwBuilder.Tests.Infrastructure.Repositories;

/// <summary>
/// Unit tests for SourceFieldRepository using InMemory database.
/// </summary>
public class SourceFieldRepositoryTests : IDisposable
{
    private readonly DwBuilderDbContext _context;
    private readonly SourceFieldRepository _repository;

    public SourceFieldRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DwBuilderDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DwBuilderDbContext(options);
        _repository = new SourceFieldRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddFieldToDatabase()
    {
        // Arrange
        var source = new Source { Name = "Test", ServerName = "s", DatabaseName = "d", LandingSchema = "l" };
        var table = new SourceTable { Source = source, SchemaName = "dbo", TableName = "Test", LandingTableName = "Test_L" };
        _context.SourceTables.Add(table);
        await _context.SaveChangesAsync();

        var field = new SourceField
        {
            SourceTableId = table.Id,
            SourceFieldName = "CustomerId",
            LandingColumnName = "CustomerId",
            DataType = "int",
            IsBusinessKey = true,
            OrdinalPosition = 1
        };

        // Act
        var result = await _repository.AddAsync(field);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetByTableIdAsync_ShouldReturnFieldsForTable()
    {
        // Arrange
        var source = new Source { Name = "Test", ServerName = "s", DatabaseName = "d", LandingSchema = "l" };
        var table1 = new SourceTable { Source = source, SchemaName = "dbo", TableName = "T1", LandingTableName = "T1_L" };
        var table2 = new SourceTable { Source = source, SchemaName = "dbo", TableName = "T2", LandingTableName = "T2_L" };
        _context.SourceTables.AddRange(table1, table2);
        await _context.SaveChangesAsync();

        var fields = new[]
        {
            new SourceField { SourceTableId = table1.Id, SourceFieldName = "F1", LandingColumnName = "F1", DataType = "int", OrdinalPosition = 1 },
            new SourceField { SourceTableId = table1.Id, SourceFieldName = "F2", LandingColumnName = "F2", DataType = "varchar", OrdinalPosition = 2 },
            new SourceField { SourceTableId = table2.Id, SourceFieldName = "F3", LandingColumnName = "F3", DataType = "int", OrdinalPosition = 1 }
        };
        _context.SourceFields.AddRange(fields);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTableIdAsync(table1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(f => f.SourceTableId == table1.Id);
    }

    [Fact]
    public async Task GetByTableIdAsync_ShouldOrderByOrdinalPosition()
    {
        // Arrange
        var source = new Source { Name = "Test", ServerName = "s", DatabaseName = "d", LandingSchema = "l" };
        var table = new SourceTable { Source = source, SchemaName = "dbo", TableName = "Test", LandingTableName = "Test_L" };
        _context.SourceTables.Add(table);
        await _context.SaveChangesAsync();

        var fields = new[]
        {
            new SourceField { SourceTableId = table.Id, SourceFieldName = "Field3", LandingColumnName = "F3", DataType = "int", OrdinalPosition = 3 },
            new SourceField { SourceTableId = table.Id, SourceFieldName = "Field1", LandingColumnName = "F1", DataType = "int", OrdinalPosition = 1 },
            new SourceField { SourceTableId = table.Id, SourceFieldName = "Field2", LandingColumnName = "F2", DataType = "int", OrdinalPosition = 2 }
        };
        _context.SourceFields.AddRange(fields);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetByTableIdAsync(table.Id)).ToList();

        // Assert
        result[0].SourceFieldName.Should().Be("Field1");
        result[1].SourceFieldName.Should().Be("Field2");
        result[2].SourceFieldName.Should().Be("Field3");
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyField()
    {
        // Arrange
        var source = new Source { Name = "Test", ServerName = "s", DatabaseName = "d", LandingSchema = "l" };
        var table = new SourceTable { Source = source, SchemaName = "dbo", TableName = "Test", LandingTableName = "Test_L" };
        var field = new SourceField
        {
            SourceTable = table,
            SourceFieldName = "Original",
            LandingColumnName = "Original",
            DataType = "int",
            IsBusinessKey = false,
            OrdinalPosition = 1
        };
        _context.SourceFields.Add(field);
        await _context.SaveChangesAsync();

        // Act
        field.IsBusinessKey = true;
        field.LandingColumnName = "Renamed";
        await _repository.UpdateAsync(field);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.SourceFields.FindAsync(field.Id);
        updated.Should().NotBeNull();
        updated!.IsBusinessKey.Should().BeTrue();
        updated.LandingColumnName.Should().Be("Renamed");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveField()
    {
        // Arrange
        var source = new Source { Name = "Test", ServerName = "s", DatabaseName = "d", LandingSchema = "l" };
        var table = new SourceTable { Source = source, SchemaName = "dbo", TableName = "Test", LandingTableName = "Test_L" };
        var field = new SourceField
        {
            SourceTable = table,
            SourceFieldName = "ToDelete",
            LandingColumnName = "ToDelete",
            DataType = "int",
            OrdinalPosition = 1
        };
        _context.SourceFields.Add(field);
        await _context.SaveChangesAsync();
        var fieldId = field.Id;

        // Act
        await _repository.DeleteAsync(fieldId);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.SourceFields.FindAsync(fieldId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task GetByTableIdAsync_WithBusinessKeys_ShouldIncludeBusinessKeyFields()
    {
        // Arrange
        var source = new Source { Name = "Test", ServerName = "s", DatabaseName = "d", LandingSchema = "l" };
        var table = new SourceTable { Source = source, SchemaName = "dbo", TableName = "Test", LandingTableName = "Test_L" };
        _context.SourceTables.Add(table);
        await _context.SaveChangesAsync();

        var fields = new[]
        {
            new SourceField { SourceTableId = table.Id, SourceFieldName = "Id", LandingColumnName = "Id", DataType = "int", IsBusinessKey = true, OrdinalPosition = 1 },
            new SourceField { SourceTableId = table.Id, SourceFieldName = "Name", LandingColumnName = "Name", DataType = "varchar", IsBusinessKey = false, OrdinalPosition = 2 }
        };
        _context.SourceFields.AddRange(fields);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTableIdAsync(table.Id);
        var businessKeys = result.Where(f => f.IsBusinessKey).ToList();

        // Assert
        businessKeys.Should().HaveCount(1);
        businessKeys.First().SourceFieldName.Should().Be("Id");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
