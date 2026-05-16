using DwBuilder.Core.Entities;

namespace DwBuilder.Core.Interfaces;

/// <summary>
/// Service for generating DDL scripts for landing and staging tables.
/// </summary>
public interface IDdlGeneratorService
{
    /// <summary>
    /// Generates a CREATE TABLE script for a landing table.
    /// </summary>
    /// <param name="sourceTable">The source table configuration</param>
    /// <param name="fields">The configured fields for the table</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The CREATE TABLE script</returns>
    Task<string> GenerateCreateLandingTableAsync(
        SourceTable sourceTable, 
        IEnumerable<SourceField> fields, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a CREATE TABLE script for a staging table.
    /// </summary>
    /// <param name="sourceTable">The source table configuration</param>
    /// <param name="fields">The configured fields for the table</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The CREATE TABLE script for staging</returns>
    Task<string> GenerateCreateStagingTableAsync(
        SourceTable sourceTable, 
        IEnumerable<SourceField> fields, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an ALTER TABLE script to add missing columns to an existing landing table.
    /// </summary>
    /// <param name="sourceTable">The source table configuration</param>
    /// <param name="fields">The configured fields for the table</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ALTER TABLE script, or empty string if no changes needed</returns>
    Task<string> GenerateAlterLandingTableAsync(
        SourceTable sourceTable, 
        IEnumerable<SourceField> fields, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a DDL script against the Data Warehouse database.
    /// </summary>
    /// <param name="ddlScript">The DDL script to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteDdlAsync(string ddlScript, CancellationToken cancellationToken = default);
}
