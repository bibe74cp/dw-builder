using System.Data;
using System.Text;
using DwBuilder.Core.Entities;
using DwBuilder.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DwBuilder.Infrastructure.Services;

/// <summary>
/// Service for generating and executing DDL scripts for landing and staging tables.
/// </summary>
public class DdlGeneratorService : IDdlGeneratorService
{
    private readonly IConfiguration _configuration;
    private readonly ISourceRepository _sourceRepository;
    private readonly ILogger<DdlGeneratorService> _logger;

    public DdlGeneratorService(
        IConfiguration configuration,
        ISourceRepository sourceRepository,
        ILogger<DdlGeneratorService> logger)
    {
        _configuration = configuration;
        _sourceRepository = sourceRepository;
        _logger = logger;
    }

    public async Task<string> GenerateCreateLandingTableAsync(
        SourceTable sourceTable,
        IEnumerable<SourceField> fields,
        CancellationToken cancellationToken = default)
    {
        var source = await _sourceRepository.GetByIdAsync(sourceTable.SourceId, cancellationToken);
        if (source == null)
        {
            throw new InvalidOperationException($"Source with ID {sourceTable.SourceId} not found.");
        }

        var landingSchema = source.LandingSchema;
        var landingTableName = sourceTable.LandingTableName;

        var orderedFields = fields.OrderBy(f => f.OrdinalPosition).ToList();
        var keyFields = orderedFields.Where(f => f.IsBusinessKey).ToList();
        var nonKeyFields = orderedFields.Where(f => !f.IsBusinessKey).ToList();

        if (!keyFields.Any())
        {
            throw new InvalidOperationException($"Table {landingTableName} must have at least one business key field.");
        }

        var sb = new StringBuilder();
        sb.AppendLine($"-- CREATE TABLE script for landing table [{landingSchema}].[{landingTableName}]");
        sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"-- Source: {source.Name} | Table: {sourceTable.SchemaName}.{sourceTable.TableName}");
        sb.AppendLine();
        sb.AppendLine($"CREATE TABLE [{landingSchema}].[{landingTableName}] (");
        sb.AppendLine();
        sb.AppendLine("    -- Business Key Columns");

        foreach (var field in keyFields)
        {
            var nullability = "NOT NULL";
            sb.AppendLine($"    [{field.LandingColumnName}] {field.SqlDataType} {nullability},");
        }

        sb.AppendLine();
        sb.AppendLine("    -- Technical Columns");
        sb.AppendLine("    [ChangeHashKey]   CHAR(64)       NOT NULL,");
        sb.AppendLine("    [InsertDatetime]  DATETIME2      NOT NULL,");
        sb.AppendLine("    [UpdateDatetime]  DATETIME2      NOT NULL,");
        sb.AppendLine("    [IsDeleted]       BIT            NOT NULL DEFAULT 0,");

        if (nonKeyFields.Any())
        {
            sb.AppendLine();
            sb.AppendLine("    -- Non-Key Columns");

            foreach (var field in nonKeyFields)
            {
                var nullability = field.IsNullable ? "NULL" : "NOT NULL";
                sb.AppendLine($"    [{field.LandingColumnName}] {field.SqlDataType} {nullability},");
            }
        }

        sb.AppendLine();
        sb.Append($"    CONSTRAINT [PK_{landingSchema}_{landingTableName}] PRIMARY KEY CLUSTERED (");
        sb.Append(string.Join(", ", keyFields.Select(f => $"[{f.LandingColumnName}]")));
        sb.AppendLine(")");
        sb.AppendLine(");");

        return sb.ToString();
    }

    public async Task<string> GenerateCreateStagingTableAsync(
        SourceTable sourceTable,
        IEnumerable<SourceField> fields,
        CancellationToken cancellationToken = default)
    {
        var source = await _sourceRepository.GetByIdAsync(sourceTable.SourceId, cancellationToken);
        if (source == null)
        {
            throw new InvalidOperationException($"Source with ID {sourceTable.SourceId} not found.");
        }

        var landingSchema = source.LandingSchema;
        var landingTableName = sourceTable.LandingTableName;
        var stagingTableName = $"stg_{landingTableName}";

        var orderedFields = fields.OrderBy(f => f.OrdinalPosition).ToList();
        var keyFields = orderedFields.Where(f => f.IsBusinessKey).ToList();
        var nonKeyFields = orderedFields.Where(f => !f.IsBusinessKey).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"-- CREATE TABLE script for staging table [{landingSchema}].[{stagingTableName}]");
        sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"-- Source: {source.Name} | Table: {sourceTable.SchemaName}.{sourceTable.TableName}");
        sb.AppendLine();
        sb.AppendLine($"CREATE TABLE [{landingSchema}].[{stagingTableName}] (");
        sb.AppendLine();
        sb.AppendLine("    -- Business Key Columns");

        foreach (var field in keyFields)
        {
            var nullability = "NOT NULL";
            sb.AppendLine($"    [{field.LandingColumnName}] {field.SqlDataType} {nullability},");
        }

        sb.AppendLine();
        sb.AppendLine("    -- Technical Columns");
        sb.AppendLine("    [ChangeHashKey]   CHAR(64)       NOT NULL,");
        sb.AppendLine("    [InsertDatetime]  DATETIME2      NOT NULL,");
        sb.AppendLine("    [UpdateDatetime]  DATETIME2      NOT NULL,");
        sb.AppendLine("    [IsDeleted]       BIT            NOT NULL DEFAULT 0");

        if (nonKeyFields.Any())
        {
            sb.AppendLine(",");
            sb.AppendLine();
            sb.AppendLine("    -- Non-Key Columns");

            var nonKeyList = nonKeyFields.ToList();
            for (int i = 0; i < nonKeyList.Count; i++)
            {
                var field = nonKeyList[i];
                var nullability = field.IsNullable ? "NULL" : "NOT NULL";
                var comma = i < nonKeyList.Count - 1 ? "," : "";
                sb.AppendLine($"    [{field.LandingColumnName}] {field.SqlDataType} {nullability}{comma}");
            }
        }

        sb.AppendLine(");");
        sb.AppendLine();
        sb.AppendLine("-- Note: Staging table has no PRIMARY KEY constraint.");
        sb.AppendLine("-- Used for TRUNCATE + BULK INSERT + MERGE pattern.");

        return sb.ToString();
    }

    public async Task<string> GenerateAlterLandingTableAsync(
        SourceTable sourceTable,
        IEnumerable<SourceField> fields,
        CancellationToken cancellationToken = default)
    {
        var source = await _sourceRepository.GetByIdAsync(sourceTable.SourceId, cancellationToken);
        if (source == null)
        {
            throw new InvalidOperationException($"Source with ID {sourceTable.SourceId} not found.");
        }

        var landingSchema = source.LandingSchema;
        var landingTableName = sourceTable.LandingTableName;

        var connectionString = _configuration.GetConnectionString("DwBuilder")
            ?? throw new InvalidOperationException("DwBuilder connection string is not configured.");

        // Check if table exists
        var tableExists = await CheckTableExistsAsync(connectionString, landingSchema, landingTableName, cancellationToken);
        if (!tableExists)
        {
            return string.Empty; // Table doesn't exist, no ALTER needed
        }

        // Get existing columns from INFORMATION_SCHEMA
        var existingColumns = await GetExistingColumnsAsync(connectionString, landingSchema, landingTableName, cancellationToken);

        var configuredColumns = fields.Select(f => f.LandingColumnName.ToUpperInvariant()).ToHashSet();

        // Technical columns that are always present
        configuredColumns.Add("CHANGEHASHKEY");
        configuredColumns.Add("INSERTDATETIME");
        configuredColumns.Add("UPDATEDATETIME");
        configuredColumns.Add("ISDELETED");

        var missingFields = fields
            .Where(f => !existingColumns.Contains(f.LandingColumnName.ToUpperInvariant()))
            .OrderBy(f => f.OrdinalPosition)
            .ToList();

        // Check for columns in DB but not in configuration
        var extraColumns = existingColumns.Except(configuredColumns).ToList();

        if (!missingFields.Any() && !extraColumns.Any())
        {
            return string.Empty; // No changes needed
        }

        var sb = new StringBuilder();
        sb.AppendLine($"-- ALTER TABLE script for [{landingSchema}].[{landingTableName}]");
        sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        if (extraColumns.Any())
        {
            sb.AppendLine("-- WARNING: The following columns exist in the database but are not configured:");
            foreach (var col in extraColumns.OrderBy(c => c))
            {
                sb.AppendLine($"-- Column [{col}] exists in DB but not in configuration");
            }
            sb.AppendLine("-- Manual intervention required if these columns should be removed.");
            sb.AppendLine();
        }

        if (missingFields.Any())
        {
            sb.AppendLine($"ALTER TABLE [{landingSchema}].[{landingTableName}]");

            for (int i = 0; i < missingFields.Count; i++)
            {
                var field = missingFields[i];
                var nullability = field.IsNullable ? "NULL" : "NOT NULL";
                var comma = i < missingFields.Count - 1 ? "," : ";";
                sb.AppendLine($"    ADD [{field.LandingColumnName}] {field.SqlDataType} {nullability}{comma}");
            }
        }

        return sb.ToString();
    }

    public async Task ExecuteDdlAsync(string ddlScript, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ddlScript))
        {
            _logger.LogWarning("Attempted to execute empty DDL script.");
            return;
        }

        var connectionString = _configuration.GetConnectionString("DwBuilder")
            ?? throw new InvalidOperationException("DwBuilder connection string is not configured.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Split script by GO statements
        var batches = SplitSqlBatches(ddlScript);

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            _logger.LogInformation("Executing DDL batch: {Batch}", batch.Substring(0, Math.Min(100, batch.Length)));

            await using var command = new SqlCommand(batch, connection)
            {
                CommandType = CommandType.Text,
                CommandTimeout = 300 // 5 minutes timeout for DDL operations
            };

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        _logger.LogInformation("DDL script executed successfully.");
    }

    private async Task<bool> CheckTableExistsAsync(
        string connectionString,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @SchemaName
              AND TABLE_NAME = @TableName";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SchemaName", schemaName);
        command.Parameters.AddWithValue("@TableName", tableName);

        var count = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        return count > 0;
    }

    private async Task<HashSet<string>> GetExistingColumnsAsync(
        string connectionString,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @SchemaName
              AND TABLE_NAME = @TableName";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SchemaName", schemaName);
        command.Parameters.AddWithValue("@TableName", tableName);

        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(reader.GetString(0).ToUpperInvariant());
        }

        return columns;
    }

    private static IEnumerable<string> SplitSqlBatches(string script)
    {
        // Split by GO statements (case-insensitive, must be on its own line)
        var lines = script.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var currentBatch = new StringBuilder();

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                if (currentBatch.Length > 0)
                {
                    yield return currentBatch.ToString();
                    currentBatch.Clear();
                }
            }
            else
            {
                currentBatch.AppendLine(line);
            }
        }

        if (currentBatch.Length > 0)
        {
            yield return currentBatch.ToString();
        }
    }
}
