namespace DwBuilder.Biml.Models;

/// <summary>
/// Root container for all BIML metadata loaded from _meta schema.
/// </summary>
public class BimlMetadata
{
    public string DwConnectionString { get; set; } = null!;
    
    public List<BimlSource> Sources { get; set; } = new();
    
    public List<BimlTable> Tables { get; set; } = new();
}

/// <summary>
/// Represents a source database system for BIML generation.
/// </summary>
public class BimlSource
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string ServerName { get; set; } = null!;
    
    public string? InstanceName { get; set; }
    
    public string DatabaseName { get; set; } = null!;
    
    public string LandingSchema { get; set; } = null!;
    
    public string? ConnectionUser { get; set; }
    
    public string? ConnectionPasswordDecrypted { get; set; }
    
    /// <summary>
    /// OLE DB connection name in BIML (e.g., "Source_ERP").
    /// </summary>
    public string ConnectionName => $"Source_{Name}";
    
    /// <summary>
    /// Builds the complete OLE DB connection string for this source.
    /// </summary>
    public string BuildConnectionString()
    {
        var builder = new System.Text.StringBuilder();
        
        // Server
        if (!string.IsNullOrWhiteSpace(InstanceName))
        {
            builder.Append($"Server={ServerName}\\{InstanceName};");
        }
        else
        {
            builder.Append($"Server={ServerName};");
        }
        
        // Database
        builder.Append($"Database={DatabaseName};");
        
        // Authentication
        if (!string.IsNullOrWhiteSpace(ConnectionUser))
        {
            builder.Append($"User Id={ConnectionUser};");
            builder.Append($"Password={ConnectionPasswordDecrypted};");
        }
        else
        {
            builder.Append("Integrated Security=True;");
        }
        
        // Trust server certificate
        builder.Append("TrustServerCertificate=True;");
        
        return builder.ToString();
    }
}

/// <summary>
/// Represents a table configured for ETL in BIML generation.
/// </summary>
public class BimlTable
{
    public int SourceTableId { get; set; }
    
    public int SourceId { get; set; }
    
    public string SourceName { get; set; } = null!;
    
    public string SchemaName { get; set; } = null!;
    
    public string TableName { get; set; } = null!;
    
    public string LandingTableName { get; set; } = null!;
    
    public string LandingSchema { get; set; } = null!;
    
    public List<BimlField> Fields { get; set; } = new();
    
    /// <summary>
    /// BIML package name (e.g., "ETL_ERP_Orders").
    /// </summary>
    public string PackageName => $"ETL_{SourceName}_{LandingTableName}";
    
    /// <summary>
    /// Source connection name in BIML (e.g., "Source_ERP").
    /// </summary>
    public string SourceConnectionName => $"Source_{SourceName}";
    
    /// <summary>
    /// Comma-separated list of source column names for SELECT statement.
    /// </summary>
    public string ColumnList => string.Join(", ", Fields.OrderBy(f => f.OrdinalPosition).Select(f => $"[{f.SourceColumnName}]"));
    
    /// <summary>
    /// Business key fields for MERGE ON clause.
    /// </summary>
    public IEnumerable<BimlField> BusinessKeyFields => Fields.Where(f => f.IsBusinessKey).OrderBy(f => f.OrdinalPosition);
    
    /// <summary>
    /// Non-business-key fields for ChangeHashKey calculation and MERGE UPDATE.
    /// </summary>
    public IEnumerable<BimlField> NonKeyFields => Fields.Where(f => !f.IsBusinessKey).OrderBy(f => f.OrdinalPosition);
}

/// <summary>
/// Represents a field/column mapping for BIML generation.
/// </summary>
public class BimlField
{
    public string SourceColumnName { get; set; } = null!;
    
    public string LandingColumnName { get; set; } = null!;
    
    public string SqlDataType { get; set; } = null!;
    
    public bool IsBusinessKey { get; set; }
    
    public bool IsNullable { get; set; }
    
    public int OrdinalPosition { get; set; }
    
    /// <summary>
    /// SSIS data type for BIML (e.g., "String", "Int32", "DateTime").
    /// </summary>
    public string SsisDataType => MapSqlToSsisDataType(SqlDataType);
    
    /// <summary>
    /// Length for string types in SSIS.
    /// </summary>
    public int? SsisLength => ExtractStringLength(SqlDataType);
    
    private static string MapSqlToSsisDataType(string sqlType)
    {
        var lower = sqlType.ToLowerInvariant();
        
        if (lower.StartsWith("varchar") || lower.StartsWith("nvarchar") || lower.StartsWith("char") || lower.StartsWith("nchar"))
            return "String";
        
        if (lower == "int")
            return "Int32";
        
        if (lower == "bigint")
            return "Int64";
        
        if (lower == "smallint")
            return "Int16";
        
        if (lower == "tinyint")
            return "Byte";
        
        if (lower == "bit")
            return "Boolean";
        
        if (lower.StartsWith("decimal") || lower.StartsWith("numeric"))
            return "Decimal";
        
        if (lower == "float")
            return "Double";
        
        if (lower == "real")
            return "Single";
        
        if (lower == "datetime" || lower == "datetime2" || lower == "smalldatetime")
            return "DateTime";
        
        if (lower == "date")
            return "Date";
        
        if (lower == "time")
            return "Time";
        
        if (lower == "datetimeoffset")
            return "DateTimeOffset";
        
        if (lower == "uniqueidentifier")
            return "Guid";
        
        // Default fallback
        return "String";
    }
    
    private static int? ExtractStringLength(string sqlType)
    {
        var lower = sqlType.ToLowerInvariant();
        
        if (!lower.Contains("varchar") && !lower.Contains("char"))
            return null;
        
        if (lower.Contains("(max)"))
            return 4000; // SSIS max for string
        
        var start = sqlType.IndexOf('(');
        var end = sqlType.IndexOf(')');
        
        if (start > 0 && end > start)
        {
            var lengthStr = sqlType.Substring(start + 1, end - start - 1);
            if (int.TryParse(lengthStr, out var length))
            {
                // NVARCHAR/NCHAR use 2 bytes per char, adjust for SSIS
                if (lower.StartsWith("nvarchar") || lower.StartsWith("nchar"))
                    return length;
                return length;
            }
        }
        
        return 50; // Default
    }
}
