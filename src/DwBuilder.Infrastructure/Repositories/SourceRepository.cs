using DwBuilder.Core.Entities;
using DwBuilder.Core.Interfaces;
using DwBuilder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DwBuilder.Infrastructure.Repositories;

/// <summary>
/// Implementation of ISourceRepository using Entity Framework Core.
/// </summary>
public class SourceRepository : ISourceRepository
{
    private readonly DwBuilderDbContext _context;
    
    public SourceRepository(DwBuilderDbContext context)
    {
        _context = context;
    }
    
    public async Task<IEnumerable<Source>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Sources
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Source?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Sources
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
    
    public async Task<Source> CreateAsync(Source source, CancellationToken cancellationToken = default)
    {
        _context.Sources.Add(source);
        await _context.SaveChangesAsync(cancellationToken);
        return source;
    }
    
    public async Task<Source> UpdateAsync(Source source, CancellationToken cancellationToken = default)
    {
        _context.Sources.Update(source);
        await _context.SaveChangesAsync(cancellationToken);
        return source;
    }
    
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var source = await GetByIdAsync(id, cancellationToken);
        if (source == null)
        {
            return false;
        }
        
        // Soft delete
        source.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
