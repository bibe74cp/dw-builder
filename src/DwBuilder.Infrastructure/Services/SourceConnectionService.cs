using System.Data;
using DwBuilder.Core.DTOs.SourceSchema;
using DwBuilder.Core.Entities;
using DwBuilder.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace DwBuilder.Infrastructure.Services;

/// <summary>
/// Implementation of SQL Server source connection service.
/// </summary>
public class SourceConnectionService : ISourceConnectionService
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SourceConnectionService> _logger;

    public SourceConnectionService(
        IEncryptionService encryptionService,
        ILogger<SourceConnectionService> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync(Source source, CancellationToken cancellationToken = default)
    {
        var connectionString = BuildConnectionString(source);
        
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            _logger.LogInformation("Successfully connected to source {SourceName}", source.Name);
            return true;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to connect to source {SourceName}", source.Name);
            throw new InvalidOperationException($"Connection failed: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<SourceTableInfo>> GetAvailableTablesAsync(Source source, CancellationToken cancellationToken = default)
    {
        var connectionString = BuildConnectionString(source);
        var tables = new List<SourceTableInfo>();

        const string query = @"
            SELECT 
                TABLE_SCHEMA AS SchemaName,
                TABLE_NAME AS TableName
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_SCHEMA, TABLE_NAME";

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(query, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(new SourceTableInfo
                {
                    SchemaName = reader.GetString(0),
                    TableName = reader.GetString(1)
                });
            }

            _logger.LogInformation("Retrieved {Count} tables from source {SourceName}", tables.Count, source.Name);
            return tables;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to retrieve tables from source {SourceName}", source.Name);
            throw new InvalidOperationException($"Failed to retrieve tables: {ex.Message}", ex);
        }
    }

    public async Task<IEnumerable<SourceColumnInfo>> GetAvailableFieldsAsync(
        Source source, 
        string schemaName, 
        string tableName, 
        CancellationToken cancellationToken = default)
    {
        var connectionString = BuildConnectionString(source);
        var columns = new List<SourceColumnInfo>();

        const string query = @"
            SELECT 
                COLUMN_NAME,
                DATA_TYPE,
                IS_NULLABLE,
                ORDINAL_POSITION,
                CHARACTER_MAXIMUM_LENGTH,
                NUMERIC_PRECISION,
                NUMERIC_SCALE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @SchemaName 
                AND TABLE_NAME = @TableName
            ORDER BY ORDINAL_POSITION";

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@SchemaName", SqlDbType.NVarChar) { Value = schemaName });
            command.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar) { Value = tableName });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add(new SourceColumnInfo
                {
                    ColumnName = reader.GetString(0),
                    DataType = reader.GetString(1),
                    IsNullable = reader.GetString(2) == "YES",
                    OrdinalPosition = reader.GetInt32(3),
                    MaxLength = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    Precision = reader.IsDBNull(5) ? null : (int)reader.GetByte(5),
                    Scale = reader.IsDBNull(6) ? null : reader.GetInt32(6)
                });
            }

            _logger.LogInformation(
                "Retrieved {Count} columns from {Schema}.{Table} in source {SourceName}", 
                columns.Count, schemaName, tableName, source.Name);
            return columns;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex, 
                "Failed to retrieve columns from {Schema}.{Table} in source {SourceName}", 
                schemaName, tableName, source.Name);
            throw new InvalidOperationException($"Failed to retrieve columns: {ex.Message}", ex);
        }
    }

    private string BuildConnectionString(Source source)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = !string.IsNullOrEmpty(source.InstanceName) 
                ? $"{source.ServerName}\\{source.InstanceName}" 
                : source.ServerName,
            InitialCatalog = source.DatabaseName,
            TrustServerCertificate = true
        };

        if (string.IsNullOrEmpty(source.ConnectionUser))
        {
            // Windows Authentication
            builder.IntegratedSecurity = true;
        }
        else
        {
            // SQL Server Authentication
            builder.UserID = source.ConnectionUser;
            builder.Password = !string.IsNullOrEmpty(source.ConnectionPasswordEncrypted)
                ? _encryptionService.Decrypt(source.ConnectionPasswordEncrypted)
                : string.Empty;
        }

        return builder.ConnectionString;
    }
}
