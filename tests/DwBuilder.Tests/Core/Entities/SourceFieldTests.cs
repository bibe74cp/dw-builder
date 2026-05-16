using DwBuilder.Core.Entities;
using FluentAssertions;

namespace DwBuilder.Tests.Core.Entities;

/// <summary>
/// Unit tests for SourceField entity.
/// </summary>
public class SourceFieldTests
{
    [Fact]
    public void SourceField_Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var field = new SourceField();

        // Assert
        field.IsActive.Should().BeTrue();
        field.IsBusinessKey.Should().BeFalse();
        field.LandingFieldName.Should().BeNull();
    }

    [Fact]
    public void SourceField_Properties_ShouldAllowSettingValues()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var field = new SourceField
        {
            Id = 1,
            SourceTableId = 10,
            SourceFieldName = "CustomerId",
            LandingFieldName = "CustomerID",
            DataType = "int",
            MaxLength = null,
            IsNullable = false,
            IsBusinessKey = true,
            OrdinalPosition = 1,
            IsActive = true,
            CreatedAt = createdAt
        };

        // Assert
        field.Id.Should().Be(1);
        field.SourceTableId.Should().Be(10);
        field.SourceFieldName.Should().Be("CustomerId");
        field.LandingFieldName.Should().Be("CustomerID");
        field.DataType.Should().Be("int");
        field.MaxLength.Should().BeNull();
        field.IsNullable.Should().BeFalse();
        field.IsBusinessKey.Should().BeTrue();
        field.OrdinalPosition.Should().Be(1);
        field.IsActive.Should().BeTrue();
        field.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void SourceField_NavigationProperty_ShouldReferenceSourceTable()
    {
        // Arrange
        var table = new SourceTable { Id = 10, TableName = "Customers" };
        var field = new SourceField { SourceTableId = table.Id, SourceTable = table };

        // Act & Assert
        field.SourceTable.Should().Be(table);
        field.SourceTableId.Should().Be(table.Id);
    }

    [Theory]
    [InlineData("varchar", 255)]
    [InlineData("nvarchar", 100)]
    [InlineData("char", 10)]
    public void SourceField_MaxLength_ShouldStoreStringFieldLengths(string dataType, int? maxLength)
    {
        // Act
        var field = new SourceField
        {
            DataType = dataType,
            MaxLength = maxLength
        };

        // Assert
        field.DataType.Should().Be(dataType);
        field.MaxLength.Should().Be(maxLength);
    }

    [Theory]
    [InlineData("int", null)]
    [InlineData("bigint", null)]
    [InlineData("datetime", null)]
    public void SourceField_MaxLength_ShouldBeNullForNonStringTypes(string dataType, int? maxLength)
    {
        // Act
        var field = new SourceField
        {
            DataType = dataType,
            MaxLength = maxLength
        };

        // Assert
        field.MaxLength.Should().BeNull();
    }

    [Fact]
    public void SourceField_IsBusinessKey_ShouldIdentifyKeyFields()
    {
        // Arrange
        var keyField = new SourceField { SourceFieldName = "CustomerId", IsBusinessKey = true };
        var regularField = new SourceField { SourceFieldName = "CustomerName", IsBusinessKey = false };

        // Assert
        keyField.IsBusinessKey.Should().BeTrue();
        regularField.IsBusinessKey.Should().BeFalse();
    }

    [Fact]
    public void SourceField_OrdinalPosition_ShouldDefineFieldOrder()
    {
        // Arrange
        var fields = new List<SourceField>
        {
            new() { SourceFieldName = "Id", OrdinalPosition = 1 },
            new() { SourceFieldName = "Name", OrdinalPosition = 2 },
            new() { SourceFieldName = "Email", OrdinalPosition = 3 }
        };

        // Act
        var sortedFields = fields.OrderBy(f => f.OrdinalPosition).ToList();

        // Assert
        sortedFields[0].SourceFieldName.Should().Be("Id");
        sortedFields[1].SourceFieldName.Should().Be("Name");
        sortedFields[2].SourceFieldName.Should().Be("Email");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("RenamedField")]
    public void SourceField_LandingFieldName_ShouldSupportRenaming(string? landingFieldName)
    {
        // Act
        var field = new SourceField
        {
            SourceFieldName = "OriginalField",
            LandingFieldName = landingFieldName
        };

        // Assert
        field.LandingFieldName.Should().Be(landingFieldName);
    }
}
