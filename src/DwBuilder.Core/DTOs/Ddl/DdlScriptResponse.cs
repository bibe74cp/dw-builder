namespace DwBuilder.Core.DTOs.Ddl;

/// <summary>
/// Response containing generated DDL scripts for a source table.
/// </summary>
public class DdlScriptResponse
{
    /// <summary>
    /// CREATE TABLE script for the landing table.
    /// </summary>
    public string LandingTableScript { get; set; } = string.Empty;

    /// <summary>
    /// CREATE TABLE script for the staging table (stg_*).
    /// </summary>
    public string StagingTableScript { get; set; } = string.Empty;

    /// <summary>
    /// ALTER TABLE script for adding missing columns to existing landing table.
    /// Empty if table doesn't exist or no changes are needed.
    /// </summary>
    public string AlterTableScript { get; set; } = string.Empty;
}
