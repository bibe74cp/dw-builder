using DwBuilder.Core.Entities;
using DwBuilder.Core.Interfaces;
using DwBuilder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DwBuilder.Infrastructure.Repositories;

/// <summary>
/// Implementation of source field repository.
/// </summary>
public class SourceFieldRepository : ISourceFieldRepository
{
    private readonly DwBuilderDbContext _context;

    public SourceFieldRepository(DwBuilderDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SourceField>> GetBySourceTableIdAsync(
        int sourceTableId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.SourceFields
            .Where(sf => sf.SourceTableId == sourceTableId)
            .OrderBy(sf => sf.OrdinalPosition)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SourceField>> UpsertBulkAsync(
        int sourceTableId, 
        IEnumerable<SourceField> sourceFields, 
        CancellationToken cancellationToken = default)
    {
        var result = new List<SourceField>();
        var now = DateTimeOffset.UtcNow;

        foreach (var field in sourceFields)
        {
            var existing = await _context.SourceFields
                .FirstOrDefaultAsync(
                    sf => sf.SourceTableId == sourceTableId 
                        && sf.SourceColumnName == field.SourceColumnName,
                    cancellationToken);

            if (existing is not null)
            {
                // Update existing
                existing.LandingColumnName = field.LandingColumnName;
                existing.SqlDataType = field.SqlDataType;
                existing.IsBusinessKey = field.IsBusinessKey;
                existing.IsNullable = field.IsNullable;
                existing.OrdinalPosition = field.OrdinalPosition;
                existing.UpdatedAt = now;
                result.Add(existing);
            }
            else
            {
                // Create new
                field.SourceTableId = sourceTableId;
                field.CreatedAt = now;
                field.UpdatedAt = now;
                _context.SourceFields.Add(field);
                result.Add(field);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }
}
