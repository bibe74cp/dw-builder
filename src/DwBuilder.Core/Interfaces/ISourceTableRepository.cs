using DwBuilder.Core.Entities;

namespace DwBuilder.Core.Interfaces;

/// <summary>
/// Repository for managing source table configurations.
/// </summary>
public interface ISourceTableRepository
{
    /// <summary>
    /// Retrieves all source tables for a specific source.
    /// </summary>
    Task<IEnumerable<SourceTable>> GetBySourceIdAsync(int sourceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a source table by ID.
    /// </summary>
    Task<SourceTable?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a source table by source ID, schema name, and table name.
    /// </summary>
    Task<SourceTable?> GetBySourceAndNamesAsync(int sourceId, string schemaName, string tableName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new source table configuration.
    /// </summary>
    Task<SourceTable> CreateAsync(SourceTable sourceTable, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing source table configuration.
    /// </summary>
    Task<SourceTable> UpdateAsync(SourceTable sourceTable, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs bulk upsert of source table configurations.
    /// </summary>
    Task<IEnumerable<SourceTable>> UpsertBulkAsync(int sourceId, IEnumerable<SourceTable> sourceTables, CancellationToken cancellationToken = default);
}
