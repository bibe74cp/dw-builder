using System.Text;
using System.Xml;
using DwBuilder.Biml.Models;

namespace DwBuilder.Biml.Templates;

/// <summary>
/// Static helper methods for generating BIML XML template fragments.
/// </summary>
public static class BimlTemplateHelpers
{
    /// <summary>
    /// Generates the &lt;Connections&gt; block with OLE DB connections for DW and all sources.
    /// </summary>
    public static string GenerateConnectionsBlock(BimlMetadata metadata)
    {
        var sb = new StringBuilder();
        sb.AppendLine("  <Connections>");
        
        // DW connection
        sb.AppendLine($"    <OleDbConnection Name=\"DW\" ConnectionString=\"{EscapeXml(metadata.DwConnectionString)}\" />");
        
        // Source connections
        foreach (var source in metadata.Sources.OrderBy(s => s.Name))
        {
            var connString = source.BuildConnectionString();
            sb.AppendLine($"    <OleDbConnection Name=\"{source.ConnectionName}\" ConnectionString=\"{EscapeXml(connString)}\" />");
        }
        
        sb.AppendLine("  </Connections>");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates a complete SSIS package for a single table ETL.
    /// </summary>
    public static string GenerateTablePackage(BimlTable table)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"    <Package Name=\"{table.PackageName}\">");
        sb.AppendLine("      <Tasks>");
        
        // Task 1: TRUNCATE staging
        sb.AppendLine(GenerateTruncateStagingTask(table));
        
        // Task 2: Data Flow
        sb.AppendLine(GenerateDataFlowTask(table));
        
        // Task 3: MERGE staging to landing
        sb.AppendLine(GenerateMergeTask(table));
        
        // Task 4: Update sync status
        sb.AppendLine(GenerateUpdateMetaTask(table));
        
        sb.AppendLine("      </Tasks>");
        
        // Event Handlers
        sb.AppendLine("      <EventHandlers>");
        sb.AppendLine(GenerateErrorHandler(table));
        sb.AppendLine("      </EventHandlers>");
        
        sb.AppendLine("    </Package>");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates TRUNCATE staging table task.
    /// </summary>
    private static string GenerateTruncateStagingTask(BimlTable table)
    {
        var sb = new StringBuilder();
        sb.AppendLine("        <ExecuteSQL Name=\"TRUNCATE Staging\" ConnectionName=\"DW\">");
        sb.AppendLine("          <DirectInput><![CDATA[");
        sb.AppendLine($"            TRUNCATE TABLE [{table.LandingSchema}].[stg_{table.LandingTableName}]");
        sb.AppendLine("          ]]></DirectInput>");
        sb.AppendLine("        </ExecuteSQL>");
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates the Data Flow Task with OLE DB Source, Script Component, and OLE DB Destination.
    /// </summary>
    public static string GenerateDataFlowTask(BimlTable table)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"        <Dataflow Name=\"Load {table.LandingSchema}.{table.LandingTableName}\">");
        sb.AppendLine("          <Transformations>");
        
        // OLE DB Source
        sb.AppendLine($"            <OleDbSource Name=\"Source\" ConnectionName=\"{table.SourceConnectionName}\">");
        sb.AppendLine("              <DirectInput><![CDATA[");
        sb.AppendLine($"                SELECT {table.ColumnList}");
        sb.AppendLine($"                FROM [{table.SchemaName}].[{table.TableName}]");
        sb.AppendLine("              ]]></DirectInput>");
        sb.AppendLine("            </OleDbSource>");
        
        // Script Component for ChangeHashKey calculation
        sb.AppendLine(GenerateScriptComponentCode(table));
        
        // OLE DB Destination (Staging)
        sb.AppendLine($"            <OleDbDestination Name=\"Staging\" ConnectionName=\"DW\">");
        sb.AppendLine($"              <ExternalTableOutput Table=\"[{table.LandingSchema}].[stg_{table.LandingTableName}]\" />");
        sb.AppendLine("            </OleDbDestination>");
        
        sb.AppendLine("          </Transformations>");
        sb.AppendLine("        </Dataflow>");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates the Script Component transformation for ChangeHashKey calculation (SHA-256).
    /// </summary>
    public static string GenerateScriptComponentCode(BimlTable table)
    {
        var sb = new StringBuilder();
        var guid = Guid.NewGuid().ToString("N");
        
        sb.AppendLine($"            <ScriptComponentTransformation Name=\"Calculate ChangeHashKey\" ProjectCoreName=\"SC_{guid}\">");
        
        // Inputs
        sb.AppendLine("              <Inputs>");
        sb.AppendLine("                <Input Name=\"Input0\">");
        sb.AppendLine("                  <Columns>");
        foreach (var field in table.Fields.OrderBy(f => f.OrdinalPosition))
        {
            var lengthAttr = field.SsisLength.HasValue ? $" Length=\"{field.SsisLength.Value}\"" : "";
            sb.AppendLine($"                    <Column SourceColumn=\"{field.SourceColumnName}\" TargetColumn=\"{field.LandingColumnName}\" DataType=\"{field.SsisDataType}\"{lengthAttr} />");
        }
        sb.AppendLine("                  </Columns>");
        sb.AppendLine("                </Input>");
        sb.AppendLine("              </Inputs>");
        
        // Outputs
        sb.AppendLine("              <Outputs>");
        sb.AppendLine("                <Output Name=\"Output0\">");
        sb.AppendLine("                  <Columns>");
        
        // All input columns
        foreach (var field in table.Fields.OrderBy(f => f.OrdinalPosition))
        {
            var lengthAttr = field.SsisLength.HasValue ? $" Length=\"{field.SsisLength.Value}\"" : "";
            sb.AppendLine($"                    <Column Name=\"{field.LandingColumnName}\" DataType=\"{field.SsisDataType}\"{lengthAttr} />");
        }
        
        // ChangeHashKey column
        sb.AppendLine("                    <Column Name=\"ChangeHashKey\" DataType=\"String\" Length=\"64\" />");
        
        sb.AppendLine("                  </Columns>");
        sb.AppendLine("                </Output>");
        sb.AppendLine("              </Outputs>");
        
        // C# Code
        sb.AppendLine("              <Code><![CDATA[");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Security.Cryptography;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine("");
        sb.AppendLine("public override void Input0_ProcessInputRow(Input0Buffer Row)");
        sb.AppendLine("{");
        
        // Build hash parts from non-key fields
        sb.AppendLine("    var parts = new string[] {");
        var nonKeyFields = table.NonKeyFields.ToList();
        for (int i = 0; i < nonKeyFields.Count; i++)
        {
            var field = nonKeyFields[i];
            var comma = i < nonKeyFields.Count - 1 ? "," : "";
            sb.AppendLine($"        Row.{field.LandingColumnName}_IsNull ? \"NULL\" : Row.{field.LandingColumnName}.ToString(){comma}");
        }
        sb.AppendLine("    };");
        sb.AppendLine("");
        sb.AppendLine("    var raw = string.Join(\"|\", parts);");
        sb.AppendLine("    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));");
        sb.AppendLine("    Row.ChangeHashKey = Convert.ToHexString(bytes).ToLowerInvariant();");
        sb.AppendLine("");
        
        // Copy all input columns to output
        foreach (var field in table.Fields.OrderBy(f => f.OrdinalPosition))
        {
            sb.AppendLine($"    Row.{field.LandingColumnName} = Row.Input0_{field.LandingColumnName};");
            sb.AppendLine($"    Row.{field.LandingColumnName}_IsNull = Row.Input0_{field.LandingColumnName}_IsNull;");
        }
        
        sb.AppendLine("}");
        sb.AppendLine("              ]]></Code>");
        sb.AppendLine("            </ScriptComponentTransformation>");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates the MERGE statement task for staging to landing.
    /// </summary>
    public static string GenerateMergeTask(BimlTable table)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("        <ExecuteSQL Name=\"MERGE Staging to Landing\" ConnectionName=\"DW\">");
        sb.AppendLine("          <DirectInput><![CDATA[");
        
        // MERGE statement
        sb.AppendLine($"            MERGE [{table.LandingSchema}].[{table.LandingTableName}] AS tgt");
        sb.AppendLine($"            USING [{table.LandingSchema}].[stg_{table.LandingTableName}] AS src");
        
        // ON clause (business keys)
        var keyConditions = table.BusinessKeyFields
            .Select(f => $"tgt.[{f.LandingColumnName}] = src.[{f.LandingColumnName}]");
        sb.AppendLine($"              ON {string.Join(" AND ", keyConditions)}");
        
        // WHEN MATCHED (update if hash changed)
        sb.AppendLine("            WHEN MATCHED AND tgt.ChangeHashKey <> src.ChangeHashKey THEN");
        sb.AppendLine("                UPDATE SET");
        sb.AppendLine("                    tgt.ChangeHashKey  = src.ChangeHashKey,");
        sb.AppendLine("                    tgt.UpdateDatetime = GETUTCDATE(),");
        sb.AppendLine("                    tgt.IsDeleted      = 0,");
        
        var nonKeyFieldsList = table.NonKeyFields.ToList();
        for (int i = 0; i < nonKeyFieldsList.Count; i++)
        {
            var field = nonKeyFieldsList[i];
            var comma = i < nonKeyFieldsList.Count - 1 ? "," : "";
            sb.AppendLine($"                    tgt.[{field.LandingColumnName}] = src.[{field.LandingColumnName}]{comma}");
        }
        
        // WHEN NOT MATCHED BY TARGET (insert)
        sb.AppendLine("            WHEN NOT MATCHED BY TARGET THEN");
        
        var allColumns = new List<string> { "ChangeHashKey", "InsertDatetime", "UpdateDatetime", "IsDeleted" };
        allColumns.AddRange(table.Fields.OrderBy(f => f.OrdinalPosition).Select(f => f.LandingColumnName));
        
        sb.AppendLine($"                INSERT ({string.Join(", ", allColumns.Select(c => $"[{c}]"))})");
        
        var allValues = new List<string> { "src.ChangeHashKey", "GETUTCDATE()", "GETUTCDATE()", "0" };
        allValues.AddRange(table.Fields.OrderBy(f => f.OrdinalPosition).Select(f => $"src.[{f.LandingColumnName}]"));
        
        sb.AppendLine($"                VALUES ({string.Join(", ", allValues)})");
        
        // WHEN NOT MATCHED BY SOURCE (soft delete)
        sb.AppendLine("            WHEN NOT MATCHED BY SOURCE AND tgt.IsDeleted = 0 THEN");
        sb.AppendLine("                UPDATE SET");
        sb.AppendLine("                    tgt.IsDeleted      = 1,");
        sb.AppendLine("                    tgt.UpdateDatetime = GETUTCDATE();");
        
        sb.AppendLine("          ]]></DirectInput>");
        sb.AppendLine("        </ExecuteSQL>");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates the task to update _meta.SourceTables sync status on success.
    /// </summary>
    public static string GenerateUpdateMetaTask(BimlTable table)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("        <ExecuteSQL Name=\"Update Sync Status\" ConnectionName=\"DW\">");
        sb.AppendLine("          <DirectInput><![CDATA[");
        sb.AppendLine("            UPDATE _meta.SourceTables");
        sb.AppendLine("            SET LastSyncAt = GETUTCDATE(),");
        sb.AppendLine("                LastSyncStatus = 'Success',");
        sb.AppendLine("                LastSyncMessage = NULL");
        sb.AppendLine($"            WHERE Id = {table.SourceTableId}");
        sb.AppendLine("          ]]></DirectInput>");
        sb.AppendLine("        </ExecuteSQL>");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates the OnError event handler to update _meta.SourceTables on failure.
    /// </summary>
    private static string GenerateErrorHandler(BimlTable table)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("        <EventHandler EventName=\"OnError\">");
        sb.AppendLine("          <Tasks>");
        sb.AppendLine("            <ExecuteSQL Name=\"Update Sync Error\" ConnectionName=\"DW\">");
        sb.AppendLine("              <DirectInput><![CDATA[");
        sb.AppendLine("                UPDATE _meta.SourceTables");
        sb.AppendLine("                SET LastSyncAt = GETUTCDATE(),");
        sb.AppendLine("                    LastSyncStatus = 'Error',");
        sb.AppendLine("                    LastSyncMessage = 'SSIS package execution failed'");
        sb.AppendLine($"                WHERE Id = {table.SourceTableId}");
        sb.AppendLine("              ]]></DirectInput>");
        sb.AppendLine("            </ExecuteSQL>");
        sb.AppendLine("          </Tasks>");
        sb.AppendLine("        </EventHandler>");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Generates a master sequence package for a source that executes all its table packages.
    /// </summary>
    public static string GenerateSequencePackage(BimlSource source, IEnumerable<BimlTable> tables)
    {
        var sb = new StringBuilder();
        var sourceTables = tables.Where(t => t.SourceId == source.Id).OrderBy(t => t.LandingTableName).ToList();
        
        if (!sourceTables.Any())
            return string.Empty;
        
        sb.AppendLine($"    <Package Name=\"Master_{source.Name}\">");
        sb.AppendLine("      <Tasks>");
        
        foreach (var table in sourceTables)
        {
            sb.AppendLine($"        <ExecutePackage Name=\"Execute {table.LandingTableName}\">");
            sb.AppendLine($"          <ExternalProjectPackage Package=\"{table.PackageName}\" />");
            sb.AppendLine("        </ExecutePackage>");
        }
        
        sb.AppendLine("      </Tasks>");
        
        // Precedence Constraints for sequential execution
        if (sourceTables.Count > 1)
        {
            sb.AppendLine("      <PrecedenceConstraints>");
            for (int i = 0; i < sourceTables.Count - 1; i++)
            {
                sb.AppendLine($"        <PrecedenceConstraint From=\"Execute {sourceTables[i].LandingTableName}\" To=\"Execute {sourceTables[i + 1].LandingTableName}\" />");
            }
            sb.AppendLine("      </PrecedenceConstraints>");
        }
        
        sb.AppendLine("    </Package>");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Escapes special XML characters.
    /// </summary>
    private static string EscapeXml(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
