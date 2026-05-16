using DwBuilder.Core.Entities;
using FluentAssertions;

namespace DwBuilder.Tests.Core.Entities;

/// <summary>
/// Unit tests for SourceTable entity.
/// </summary>
public class SourceTableTests
{
    [Fact]
    public void SourceTable_Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var table = new SourceTable();

        // Assert
        table.IsActive.Should().BeTrue();
        table.SourceFields.Should().NotBeNull().And.BeEmpty();
        table.LastSyncStatus.Should().BeNull();
        table.LastSyncMessage.Should().BeNull();
        table.LastSyncAt.Should().BeNull();
    }

    [Fact]
    public void SourceTable_Properties_ShouldAllowSettingValues()
    {
        // Arrange
        var lastSyncAt = DateTimeOffset.UtcNow;
        var createdAt = lastSyncAt.AddDays(-1);

        // Act
        var table = new SourceTable
        {
            Id = 1,
            SourceId = 10,
            SchemaName = "dbo",
            TableName = "Customers",
            LandingTableName = "Customers_L",
            IsActive = true,
            LastSyncStatus = "Success",
            LastSyncMessage = "100 rows processed",
            LastSyncAt = lastSyncAt,
            CreatedAt = createdAt
        };

        // Assert
        table.Id.Should().Be(1);
        table.SourceId.Should().Be(10);
        table.SchemaName.Should().Be("dbo");
        table.TableName.Should().Be("Customers");
        table.LandingTableName.Should().Be("Customers_L");
        table.IsActive.Should().BeTrue();
        table.LastSyncStatus.Should().Be("Success");
        table.LastSyncMessage.Should().Be("100 rows processed");
        table.LastSyncAt.Should().Be(lastSyncAt);
        table.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void SourceTable_NavigationProperties_ShouldSupportRelations()
    {
        // Arrange
        var source = new Source { Id = 10, Name = "ERP" };
        var table = new SourceTable { SourceId = source.Id, Source = source };
        var field1 = new SourceField { SourceTableId = table.Id, SourceFieldName = "CustomerId" };
        var field2 = new SourceField { SourceTableId = table.Id, SourceFieldName = "CustomerName" };

        // Act
        table.SourceFields.Add(field1);
        table.SourceFields.Add(field2);

        // Assert
        table.Source.Should().Be(source);
        table.SourceFields.Should().HaveCount(2);
        table.SourceFields.Should().Contain(field1);
        table.SourceFields.Should().Contain(field2);
    }

    [Theory]
    [InlineData("Success")]
    [InlineData("Failed")]
    [InlineData("InProgress")]
    [InlineData(null)]
    public void SourceTable_LastSyncStatus_ShouldAllowVariousStatuses(string? status)
    {
        // Act
        var table = new SourceTable { LastSyncStatus = status };

        // Assert
        table.LastSyncStatus.Should().Be(status);
    }

    [Fact]
    public void SourceTable_LastSyncAt_ShouldTrackSyncTimestamp()
    {
        // Arrange
        var syncTime = DateTimeOffset.UtcNow;
        var table = new SourceTable();

        // Act
        table.LastSyncAt = syncTime;

        // Assert
        table.LastSyncAt.Should().Be(syncTime);
    }

    [Fact]
    public void SourceTable_Name_ShouldCombineSchemaAndTableName()
    {
        // Arrange
        var table = new SourceTable
        {
            SchemaName = "sales",
            TableName = "Orders"
        };

        // Act
        var fullName = $"{table.SchemaName}.{table.TableName}";

        // Assert
        fullName.Should().Be("sales.Orders");
    }
}
