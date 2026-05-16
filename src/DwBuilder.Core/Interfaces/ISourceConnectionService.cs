using DwBuilder.Core.DTOs.SourceSchema;
using DwBuilder.Core.Entities;

namespace DwBuilder.Core.Interfaces;

/// <summary>
/// Service for connecting to source SQL Server databases and reading schema information.
/// </summary>
public interface ISourceConnectionService
{
    /// <summary>
    /// Tests the connection to a source database.
    /// </summary>
    /// <param name="source">The source configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection is successful, otherwise throws exception.</returns>
    Task<bool> TestConnectionAsync(Source source, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all user tables from the source database.
    /// </summary>
    /// <param name="source">The source configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of tables with schema and table names.</returns>
    Task<IEnumerable<SourceTableInfo>> GetAvailableTablesAsync(Source source, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all columns for a specific table in the source database.
    /// </summary>
    /// <param name="source">The source configuration.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="tableName">The table name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of columns with metadata.</returns>
    Task<IEnumerable<SourceColumnInfo>> GetAvailableFieldsAsync(Source source, string schemaName, string tableName, CancellationToken cancellationToken = default);
}
