using DwBuilder.Core.Entities;
using FluentAssertions;

namespace DwBuilder.Tests.Core.Entities;

/// <summary>
/// Unit tests for Source entity.
/// </summary>
public class SourceTests
{
    [Fact]
    public void Source_Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var source = new Source();

        // Assert
        source.IsActive.Should().BeTrue();
        source.SourceTables.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Source_Properties_ShouldAllowSettingValues()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow;
        var updatedAt = createdAt.AddMinutes(5);

        // Act
        var source = new Source
        {
            Id = 1,
            Name = "ERP Source",
            ServerName = "sql-server-01",
            InstanceName = "PROD",
            DatabaseName = "ERP_DB",
            LandingSchema = "erp_landing",
            ConnectionUser = "dwbuilder_user",
            ConnectionPasswordEncrypted = "IV:EncryptedPassword",
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        source.Id.Should().Be(1);
        source.Name.Should().Be("ERP Source");
        source.ServerName.Should().Be("sql-server-01");
        source.InstanceName.Should().Be("PROD");
        source.DatabaseName.Should().Be("ERP_DB");
        source.LandingSchema.Should().Be("erp_landing");
        source.ConnectionUser.Should().Be("dwbuilder_user");
        source.ConnectionPasswordEncrypted.Should().Be("IV:EncryptedPassword");
        source.IsActive.Should().BeTrue();
        source.CreatedAt.Should().Be(createdAt);
        source.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Source_NavigationProperties_ShouldSupportCollectionOperations()
    {
        // Arrange
        var source = new Source();
        var table1 = new SourceTable { Id = 1, SourceId = source.Id, Name = "Customers" };
        var table2 = new SourceTable { Id = 2, SourceId = source.Id, Name = "Orders" };

        // Act
        source.SourceTables.Add(table1);
        source.SourceTables.Add(table2);

        // Assert
        source.SourceTables.Should().HaveCount(2);
        source.SourceTables.Should().Contain(table1);
        source.SourceTables.Should().Contain(table2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Source_InstanceName_ShouldAllowNullOrEmpty(string? instanceName)
    {
        // Act
        var source = new Source { InstanceName = instanceName };

        // Assert
        source.InstanceName.Should().Be(instanceName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Source_ConnectionUser_ShouldAllowNullOrEmpty(string? connectionUser)
    {
        // Act
        var source = new Source { ConnectionUser = connectionUser };

        // Assert
        source.ConnectionUser.Should().Be(connectionUser);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Source_ConnectionPasswordEncrypted_ShouldAllowNullOrEmpty(string? password)
    {
        // Act
        var source = new Source { ConnectionPasswordEncrypted = password };

        // Assert
        source.ConnectionPasswordEncrypted.Should().Be(password);
    }
}
