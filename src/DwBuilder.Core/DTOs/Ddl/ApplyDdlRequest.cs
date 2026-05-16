namespace DwBuilder.Core.DTOs.Ddl;

/// <summary>
/// Request to apply DDL scripts to the Data Warehouse.
/// </summary>
public class ApplyDdlRequest
{
    /// <summary>
    /// Execute the CREATE TABLE script for the landing table.
    /// </summary>
    public bool ExecuteCreate { get; set; }

    /// <summary>
    /// Execute the CREATE TABLE script for the staging table.
    /// </summary>
    public bool ExecuteStaging { get; set; }

    /// <summary>
    /// Execute the ALTER TABLE script for adding missing columns.
    /// </summary>
    public bool ExecuteAlter { get; set; }
}
