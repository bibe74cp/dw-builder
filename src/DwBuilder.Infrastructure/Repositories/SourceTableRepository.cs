using DwBuilder.Core.Entities;
using DwBuilder.Core.Interfaces;
using DwBuilder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DwBuilder.Infrastructure.Repositories;

/// <summary>
/// Implementation of source table repository.
/// </summary>
public class SourceTableRepository : ISourceTableRepository
{
    private readonly DwBuilderDbContext _context;

    public SourceTableRepository(DwBuilderDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SourceTable>> GetBySourceIdAsync(int sourceId, CancellationToken cancellationToken = default)
    {
        return await _context.SourceTables
            .Where(st => st.SourceId == sourceId)
            .OrderBy(st => st.SchemaName)
            .ThenBy(st => st.TableName)
            .ToListAsync(cancellationToken);
    }

    public async Task<SourceTable?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.SourceTables
            .FirstOrDefaultAsync(st => st.Id == id, cancellationToken);
    }

    public async Task<SourceTable?> GetBySourceAndNamesAsync(
        int sourceId, 
        string schemaName, 
        string tableName, 
        CancellationToken cancellationToken = default)
    {
        return await _context.SourceTables
            .FirstOrDefaultAsync(
                st => st.SourceId == sourceId 
                    && st.SchemaName == schemaName 
                    && st.TableName == tableName,
                cancellationToken);
    }

    public async Task<SourceTable> CreateAsync(SourceTable sourceTable, CancellationToken cancellationToken = default)
    {
        sourceTable.CreatedAt = DateTimeOffset.UtcNow;
        sourceTable.UpdatedAt = DateTimeOffset.UtcNow;

        _context.SourceTables.Add(sourceTable);
        await _context.SaveChangesAsync(cancellationToken);

        return sourceTable;
    }

    public async Task<SourceTable> UpdateAsync(SourceTable sourceTable, CancellationToken cancellationToken = default)
    {
        sourceTable.UpdatedAt = DateTimeOffset.UtcNow;

        _context.SourceTables.Update(sourceTable);
        await _context.SaveChangesAsync(cancellationToken);

        return sourceTable;
    }

    public async Task<IEnumerable<SourceTable>> UpsertBulkAsync(
        int sourceId, 
        IEnumerable<SourceTable> sourceTables, 
        CancellationToken cancellationToken = default)
    {
        var result = new List<SourceTable>();
        var now = DateTimeOffset.UtcNow;

        foreach (var table in sourceTables)
        {
            var existing = await GetBySourceAndNamesAsync(
                sourceId, 
                table.SchemaName, 
                table.TableName, 
                cancellationToken);

            if (existing is not null)
            {
                // Update existing
                existing.LandingTableName = table.LandingTableName;
                existing.IsActive = table.IsActive;
                existing.UpdatedAt = now;
                result.Add(existing);
            }
            else
            {
                // Create new
                table.SourceId = sourceId;
                table.CreatedAt = now;
                table.UpdatedAt = now;
                _context.SourceTables.Add(table);
                result.Add(table);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return result;
    }
}
