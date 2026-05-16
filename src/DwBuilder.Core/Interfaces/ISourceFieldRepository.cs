using DwBuilder.Core.Entities;

namespace DwBuilder.Core.Interfaces;

/// <summary>
/// Repository for managing source field configurations.
/// </summary>
public interface ISourceFieldRepository
{
    /// <summary>
    /// Retrieves all source fields for a specific source table.
    /// </summary>
    Task<IEnumerable<SourceField>> GetBySourceTableIdAsync(int sourceTableId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs bulk upsert of source field configurations.
    /// </summary>
    Task<IEnumerable<SourceField>> UpsertBulkAsync(int sourceTableId, IEnumerable<SourceField> sourceFields, CancellationToken cancellationToken = default);
}
