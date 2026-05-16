using DwBuilder.Core.Entities;

namespace DwBuilder.Core.Interfaces;

/// <summary>
/// Repository interface for Source entity CRUD operations.
/// </summary>
public interface ISourceRepository
{
    Task<IEnumerable<Source>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    
    Task<Source?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    Task<Source> CreateAsync(Source source, CancellationToken cancellationToken = default);
    
    Task<Source> UpdateAsync(Source source, CancellationToken cancellationToken = default);
    
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
