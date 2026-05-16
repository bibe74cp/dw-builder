using DwBuilder.Core.DTOs.SourceFields;
using DwBuilder.Core.DTOs.SourceSchema;
using DwBuilder.Core.DTOs.SourceTables;
using DwBuilder.Core.Entities;
using DwBuilder.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DwBuilder.Api.Controllers;

/// <summary>
/// API controller for managing source table and field configurations.
/// </summary>
[ApiController]
[Route("api/v1/sources/{sourceId:int}/tables")]
[Authorize]
public class SourceTablesController : ControllerBase
{
    private readonly ISourceRepository _sourceRepository;
    private readonly ISourceTableRepository _sourceTableRepository;
    private readonly ISourceFieldRepository _sourceFieldRepository;
    private readonly ISourceConnectionService _sourceConnectionService;
    private readonly ILogger<SourceTablesController> _logger;

    public SourceTablesController(
        ISourceRepository sourceRepository,
        ISourceTableRepository sourceTableRepository,
        ISourceFieldRepository sourceFieldRepository,
        ISourceConnectionService sourceConnectionService,
        ILogger<SourceTablesController> logger)
    {
        _sourceRepository = sourceRepository;
        _sourceTableRepository = sourceTableRepository;
        _sourceFieldRepository = sourceFieldRepository;
        _sourceConnectionService = sourceConnectionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all configured tables for a source.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SourceTableDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SourceTableDto>>> GetTables(
        int sourceId,
        CancellationToken cancellationToken)
    {
        var source = await _sourceRepository.GetByIdAsync(sourceId, cancellationToken);
        if (source is null || !source.IsActive)
            return NotFound();

        var tables = await _sourceTableRepository.GetBySourceIdAsync(sourceId, cancellationToken);
        return Ok(tables.Select(ToTableDto));
    }

    /// <summary>
    /// Bulk upserts table configurations for a source.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(IEnumerable<SourceTableDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SourceTableDto>>> UpdateTables(
        int sourceId,
        [FromBody] BulkUpdateTablesRequest request,
        CancellationToken cancellationToken)
    {
        var source = await _sourceRepository.GetByIdAsync(sourceId, cancellationToken);
        if (source is null || !source.IsActive)
            return NotFound();

        var sourceTables = request.Tables.Select(item => new SourceTable
        {
            SchemaName = item.SchemaName,
            TableName = item.TableName,
            LandingTableName = item.LandingTableName,
            IsActive = item.IsActive
        });

        var updated = await _sourceTableRepository.UpsertBulkAsync(sourceId, sourceTables, cancellationToken);
        
        _logger.LogInformation(
            "Bulk updated {Count} tables for source {SourceId}", 
            request.Tables.Count, 
            sourceId);

        return Ok(updated.Select(ToTableDto));
    }

    /// <summary>
    /// Gets available fields/columns for a specific source table from the live database.
    /// </summary>
    [HttpGet("{tableId:int}/available-fields")]
    [ProducesResponseType(typeof(IEnumerable<SourceColumnInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SourceColumnInfo>>> GetAvailableFields(
        int sourceId,
        int tableId,
        CancellationToken cancellationToken)
    {
        var sourceTable = await _sourceTableRepository.GetByIdAsync(tableId, cancellationToken);
        if (sourceTable is null || sourceTable.SourceId != sourceId)
            return NotFound();

        var source = await _sourceRepository.GetByIdAsync(sourceId, cancellationToken);
        if (source is null || !source.IsActive)
            return NotFound();

        try
        {
            var fields = await _sourceConnectionService.GetAvailableFieldsAsync(
                source,
                sourceTable.SchemaName,
                sourceTable.TableName,
                cancellationToken);

            return Ok(fields);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to retrieve fields for table {TableId}", tableId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Gets all configured fields for a source table.
    /// </summary>
    [HttpGet("{tableId:int}/fields")]
    [ProducesResponseType(typeof(IEnumerable<SourceFieldDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SourceFieldDto>>> GetFields(
        int sourceId,
        int tableId,
        CancellationToken cancellationToken)
    {
        var sourceTable = await _sourceTableRepository.GetByIdAsync(tableId, cancellationToken);
        if (sourceTable is null || sourceTable.SourceId != sourceId)
            return NotFound();

        var fields = await _sourceFieldRepository.GetBySourceTableIdAsync(tableId, cancellationToken);
        return Ok(fields.Select(ToFieldDto));
    }

    /// <summary>
    /// Bulk upserts field configurations for a source table.
    /// </summary>
    [HttpPut("{tableId:int}/fields")]
    [ProducesResponseType(typeof(IEnumerable<SourceFieldDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SourceFieldDto>>> UpdateFields(
        int sourceId,
        int tableId,
        [FromBody] BulkUpdateFieldsRequest request,
        CancellationToken cancellationToken)
    {
        var sourceTable = await _sourceTableRepository.GetByIdAsync(tableId, cancellationToken);
        if (sourceTable is null || sourceTable.SourceId != sourceId)
            return NotFound();

        // Validate at least one business key
        if (!request.Fields.Any(f => f.IsBusinessKey))
        {
            return BadRequest(new 
            { 
                success = false, 
                message = "At least one field must be marked as business key" 
            });
        }

        // Validate unique ordinal positions
        var ordinalPositions = request.Fields.Select(f => f.OrdinalPosition).ToList();
        if (ordinalPositions.Count != ordinalPositions.Distinct().Count())
        {
            return BadRequest(new 
            { 
                success = false, 
                message = "OrdinalPosition must be unique for all fields" 
            });
        }

        var sourceFields = request.Fields.Select(item => new SourceField
        {
            SourceColumnName = item.SourceColumnName,
            LandingColumnName = item.LandingColumnName,
            SqlDataType = item.SqlDataType,
            IsBusinessKey = item.IsBusinessKey,
            IsNullable = item.IsNullable,
            OrdinalPosition = item.OrdinalPosition
        });

        var updated = await _sourceFieldRepository.UpsertBulkAsync(tableId, sourceFields, cancellationToken);
        
        _logger.LogInformation(
            "Bulk updated {Count} fields for table {TableId}", 
            request.Fields.Count, 
            tableId);

        return Ok(updated.Select(ToFieldDto));
    }

    private static SourceTableDto ToTableDto(SourceTable table) => new()
    {
        Id = table.Id,
        SourceId = table.SourceId,
        SchemaName = table.SchemaName,
        TableName = table.TableName,
        LandingTableName = table.LandingTableName,
        IsActive = table.IsActive,
        LastSyncAt = table.LastSyncAt,
        LastSyncStatus = table.LastSyncStatus,
        CreatedAt = table.CreatedAt,
        UpdatedAt = table.UpdatedAt
    };

    private static SourceFieldDto ToFieldDto(SourceField field) => new()
    {
        Id = field.Id,
        SourceTableId = field.SourceTableId,
        SourceColumnName = field.SourceColumnName,
        LandingColumnName = field.LandingColumnName,
        SqlDataType = field.SqlDataType,
        IsBusinessKey = field.IsBusinessKey,
        IsNullable = field.IsNullable,
        OrdinalPosition = field.OrdinalPosition,
        CreatedAt = field.CreatedAt,
        UpdatedAt = field.UpdatedAt
    };
}
