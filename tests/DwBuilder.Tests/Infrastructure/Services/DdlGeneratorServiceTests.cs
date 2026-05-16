using DwBuilder.Core.Entities;
using DwBuilder.Core.Interfaces;
using DwBuilder.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace DwBuilder.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for DdlGeneratorService.
/// </summary>
public class DdlGeneratorServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ISourceRepository> _sourceRepositoryMock;
    private readonly Mock<ILogger<DdlGeneratorService>> _loggerMock;
    private readonly DdlGeneratorService _service;

    public DdlGeneratorServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _sourceRepositoryMock = new Mock<ISourceRepository>();
        _loggerMock = new Mock<ILogger<DdlGeneratorService>>();
        _service = new DdlGeneratorService(
            _configurationMock.Object,
            _sourceRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateCreateLandingTableAsync_ShouldGenerateValidDDL()
    {
        // Arrange
        var source = new Source
        {
            Id = 1,
            Name = "ERP",
            LandingSchema = "erp_landing"
        };

        var sourceTable = new SourceTable
        {
            Id = 10,
            SourceId = 1,
            SchemaName = "dbo",
            TableName = "Customers",
            LandingTableName = "Customers_L"
        };

        var fields = new List<SourceField>
        {
            new()
            {
                SourceFieldName = "CustomerId",
                LandingColumnName = "CustomerId",
                SqlDataType = "INT",
                IsBusinessKey = true,
                IsNullable = false,
                OrdinalPosition = 1
            },
            new()
            {
                SourceFieldName = "CustomerName",
                LandingColumnName = "CustomerName",
                SqlDataType = "NVARCHAR(100)",
                IsBusinessKey = false,
                IsNullable = false,
                OrdinalPosition = 2
            }
        };

        _sourceRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        // Act
        var ddl = await _service.GenerateCreateLandingTableAsync(sourceTable, fields);

        // Assert
        ddl.Should().NotBeNullOrEmpty();
        ddl.Should().Contain("CREATE TABLE [erp_landing].[Customers_L]");
        ddl.Should().Contain("[CustomerId] INT NOT NULL");
        ddl.Should().Contain("[CustomerName] NVARCHAR(100) NOT NULL");
        ddl.Should().Contain("[ChangeHashKey] CHAR(64) NOT NULL");
        ddl.Should().Contain("[InsertDatetime] DATETIME2 NOT NULL");
        ddl.Should().Contain("[UpdateDatetime] DATETIME2 NOT NULL");
        ddl.Should().Contain("[IsDeleted] BIT NOT NULL");
        ddl.Should().Contain("CONSTRAINT [PK_erp_landing_Customers_L] PRIMARY KEY CLUSTERED ([CustomerId])");
    }

    [Fact]
    public async Task GenerateCreateLandingTableAsync_WithoutBusinessKey_ShouldThrowException()
    {
        // Arrange
        var source = new Source { Id = 1, LandingSchema = "test_landing" };
        var sourceTable = new SourceTable { SourceId = 1, LandingTableName = "Test_L" };
        var fields = new List<SourceField>
        {
            new() { SourceFieldName = "Field1", IsBusinessKey = false, OrdinalPosition = 1 }
        };

        _sourceRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        // Act
        var act = async () => await _service.GenerateCreateLandingTableAsync(sourceTable, fields);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must have at least one business key field*");
    }

    [Fact]
    public async Task GenerateCreateLandingTableAsync_WithCompositeKey_ShouldGenerateCorrectPK()
    {
        // Arrange
        var source = new Source { Id = 1, LandingSchema = "landing" };
        var sourceTable = new SourceTable { SourceId = 1, LandingTableName = "Orders_L" };
        var fields = new List<SourceField>
        {
            new() { LandingColumnName = "OrderId", SqlDataType = "INT", IsBusinessKey = true, OrdinalPosition = 1 },
            new() { LandingColumnName = "OrderLineId", SqlDataType = "INT", IsBusinessKey = true, OrdinalPosition = 2 }
        };

        _sourceRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        // Act
        var ddl = await _service.GenerateCreateLandingTableAsync(sourceTable, fields);

        // Assert
        ddl.Should().Contain("PRIMARY KEY CLUSTERED ([OrderId], [OrderLineId])");
    }

    [Fact]
    public async Task GenerateCreateLandingTableAsync_FieldOrdering_ShouldRespectOrdinalPosition()
    {
        // Arrange
        var source = new Source { Id = 1, LandingSchema = "landing" };
        var sourceTable = new SourceTable { SourceId = 1, LandingTableName = "Test_L" };
        var fields = new List<SourceField>
        {
            new() { LandingColumnName = "Field3", IsBusinessKey = true, OrdinalPosition = 3, SqlDataType = "INT" },
            new() { LandingColumnName = "Field1", IsBusinessKey = true, OrdinalPosition = 1, SqlDataType = "INT" },
            new() { LandingColumnName = "Field2", IsBusinessKey = true, OrdinalPosition = 2, SqlDataType = "INT" }
        };

        _sourceRepositoryMock
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        // Act
        var ddl = await _service.GenerateCreateLandingTableAsync(sourceTable, fields);

        // Assert
        var field1Index = ddl.IndexOf("[Field1]");
        var field2Index = ddl.IndexOf("[Field2]");
        var field3Index = ddl.IndexOf("[Field3]");

        field1Index.Should().BeLessThan(field2Index);
        field2Index.Should().BeLessThan(field3Index);
    }
}
