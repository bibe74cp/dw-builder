using System.Data;
using System.Text;
using DwBuilder.Biml.Models;
using DwBuilder.Biml.Templates;
using DwBuilder.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DwBuilder.Biml;

/// <summary>
/// Service for generating BIML master template files.
/// Reads metadata from _meta schema and generates a complete .biml file with BimlScript.
/// </summary>
public class BimlGenerator : IBimlGenerator
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<BimlGenerator> _logger;
    
    public BimlGenerator(
        IEncryptionService encryptionService,
        ILogger<BimlGenerator> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }
    
    /// <summary>
    /// Generates a complete BIML master template file.
    /// </summary>
    public async Task<string> GenerateBimlAsync(string dwConnectionString, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting BIML generation from _meta schema");
        
        // Load metadata from DW
        var metadata = await LoadMetadataAsync(dwConnectionString, cancellationToken);
        
        _logger.LogInformation(
            "Loaded metadata: {SourceCount} sources, {TableCount} tables",
            metadata.Sources.Count,
            metadata.Tables.Count);
        
        // Generate BIML XML
        var bimlContent = GenerateBimlXml(metadata);
        
        _logger.LogInformation("BIML generation completed successfully");
        
        return bimlContent;
    }
    
    /// <summary>
    /// Loads all active metadata from _meta schema.
    /// </summary>
    private async Task<BimlMetadata> LoadMetadataAsync(string dwConnectionString, CancellationToken cancellationToken)
    {
        var metadata = new BimlMetadata
        {
            DwConnectionString = dwConnectionString
        };
        
        await using var connection = new SqlConnection(dwConnectionString);
        await connection.OpenAsync(cancellationToken);
        
        // Load sources
        metadata.Sources = await LoadSourcesAsync(connection, cancellationToken);
        
        // Load tables with fields
        metadata.Tables = await LoadTablesAsync(connection, cancellationToken);
        
        return metadata;
    }
    
    /// <summary>
    /// Loads all active sources from _meta.Sources.
    /// </summary>
    private async Task<List<BimlSource>> LoadSourcesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var sources = new List<BimlSource>();
        
        var sql = @"
            SELECT 
                Id,
                Name,
                ServerName,
                InstanceName,
                DatabaseName,
                LandingSchema,
                ConnectionUser,
                ConnectionPasswordEncrypted
            FROM _meta.Sources
            WHERE IsActive = 1
            ORDER BY Name";
        
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var source = new BimlSource
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                ServerName = reader.GetString(2),
                InstanceName = reader.IsDBNull(3) ? null : reader.GetString(3),
                DatabaseName = reader.GetString(4),
                LandingSchema = reader.GetString(5),
                ConnectionUser = reader.IsDBNull(6) ? null : reader.GetString(6),
            };
            
            // Decrypt password if present
            if (!reader.IsDBNull(7))
            {
                var encryptedPassword = reader.GetString(7);
                source.ConnectionPasswordDecrypted = _encryptionService.Decrypt(encryptedPassword);
            }
            
            sources.Add(source);
        }
        
        return sources;
    }
    
    /// <summary>
    /// Loads all active tables with their fields from _meta.SourceTables and _meta.SourceFields.
    /// </summary>
    private async Task<List<BimlTable>> LoadTablesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var tables = new List<BimlTable>();
        
        // Load tables
        var tableSql = @"
            SELECT 
                st.Id,
                st.SourceId,
                s.Name AS SourceName,
                st.SchemaName,
                st.TableName,
                st.LandingTableName,
                s.LandingSchema
            FROM _meta.SourceTables st
            INNER JOIN _meta.Sources s ON st.SourceId = s.Id
            WHERE st.IsActive = 1 AND s.IsActive = 1
            ORDER BY s.Name, st.LandingTableName";
        
        await using (var command = new SqlCommand(tableSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var table = new BimlTable
                {
                    SourceTableId = reader.GetInt32(0),
                    SourceId = reader.GetInt32(1),
                    SourceName = reader.GetString(2),
                    SchemaName = reader.GetString(3),
                    TableName = reader.GetString(4),
                    LandingTableName = reader.GetString(5),
                    LandingSchema = reader.GetString(6)
                };
                
                tables.Add(table);
            }
        }
        
        // Load fields for each table
        foreach (var table in tables)
        {
            table.Fields = await LoadFieldsAsync(connection, table.SourceTableId, cancellationToken);
        }
        
        return tables;
    }
    
    /// <summary>
    /// Loads all fields for a specific table from _meta.SourceFields.
    /// </summary>
    private async Task<List<BimlField>> LoadFieldsAsync(SqlConnection connection, int sourceTableId, CancellationToken cancellationToken)
    {
        var fields = new List<BimlField>();
        
        var sql = @"
            SELECT 
                SourceColumnName,
                LandingColumnName,
                SqlDataType,
                IsBusinessKey,
                IsNullable,
                OrdinalPosition
            FROM _meta.SourceFields
            WHERE SourceTableId = @SourceTableId
            ORDER BY OrdinalPosition";
        
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@SourceTableId", sourceTableId);
        
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var field = new BimlField
            {
                SourceColumnName = reader.GetString(0),
                LandingColumnName = reader.GetString(1),
                SqlDataType = reader.GetString(2),
                IsBusinessKey = reader.GetBoolean(3),
                IsNullable = reader.GetBoolean(4),
                OrdinalPosition = reader.GetInt32(5)
            };
            
            fields.Add(field);
        }
        
        return fields;
    }
    
    /// <summary>
    /// Generates the complete BIML XML document.
    /// </summary>
    private string GenerateBimlXml(BimlMetadata metadata)
    {
        var sb = new StringBuilder();
        
        // XML header
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<Biml xmlns=\"http://schemas.varigence.com/biml.xsd\">");
        sb.AppendLine();
        
        // Connections block
        sb.AppendLine(BimlTemplateHelpers.GenerateConnectionsBlock(metadata));
        sb.AppendLine();
        
        // Packages block
        sb.AppendLine("  <Packages>");
        sb.AppendLine();
        
        // Individual table packages
        foreach (var table in metadata.Tables.OrderBy(t => t.SourceName).ThenBy(t => t.LandingTableName))
        {
            sb.AppendLine(BimlTemplateHelpers.GenerateTablePackage(table));
            sb.AppendLine();
        }
        
        // Master sequence packages per source
        foreach (var source in metadata.Sources.OrderBy(s => s.Name))
        {
            var sequencePackage = BimlTemplateHelpers.GenerateSequencePackage(source, metadata.Tables);
            if (!string.IsNullOrWhiteSpace(sequencePackage))
            {
                sb.AppendLine(sequencePackage);
                sb.AppendLine();
            }
        }
        
        sb.AppendLine("  </Packages>");
        sb.AppendLine();
        
        // Close Biml root
        sb.AppendLine("</Biml>");
        
        return sb.ToString();
    }
}
