using DwBuilder.Core.DTOs.Ddl;
using DwBuilder.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DwBuilder.Api.Controllers;

/// <summary>
/// Controller for generating and applying DDL scripts.
/// </summary>
[ApiController]
[Route("api/v1/sources")]
[Authorize]
public class DdlController : ControllerBase
{
    private readonly ISourceTableRepository _sourceTableRepository;
    private readonly ISourceFieldRepository _sourceFieldRepository;
    private readonly IDdlGeneratorService _ddlGeneratorService;
    private readonly ILogger<DdlController> _logger;

    public DdlController(
        ISourceTableRepository sourceTableRepository,
        ISourceFieldRepository sourceFieldRepository,
        IDdlGeneratorService ddlGeneratorService,
        ILogger<DdlController> logger)
    {
        _sourceTableRepository = sourceTableRepository;
        _sourceFieldRepository = sourceFieldRepository;
        _ddlGeneratorService = ddlGeneratorService;
        _logger = logger;
    }

    /// <summary>
    /// Generate DDL scripts for a source table (landing + staging + alter).
    /// </summary>
    [HttpGet("{sourceId}/tables/{tableId}/ddl")]
    [ProducesResponseType(typeof(DdlScriptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDdlScripts(int sourceId, int tableId, CancellationToken cancellationToken)
    {
        try
        {
            // Load source table and verify it belongs to the specified source
            var sourceTable = await _sourceTableRepository.GetByIdAsync(tableId, cancellationToken);
            if (sourceTable == null)
            {
                return NotFound(new { message = $"Source table with ID {tableId} not found." });
            }

            if (sourceTable.SourceId != sourceId)
            {
                return BadRequest(new { message = $"Table {tableId} does not belong to source {sourceId}." });
            }

            // Load all fields for this table
            var fields = await _sourceFieldRepository.GetBySourceTableIdAsync(tableId, cancellationToken);
            var fieldList = fields.ToList();

            if (!fieldList.Any())
            {
                return BadRequest(new { message = $"Table {tableId} has no configured fields. Cannot generate DDL." });
            }

            // Generate the three DDL scripts
            var landingScript = await _ddlGeneratorService.GenerateCreateLandingTableAsync(
                sourceTable, fieldList, cancellationToken);

            var stagingScript = await _ddlGeneratorService.GenerateCreateStagingTableAsync(
                sourceTable, fieldList, cancellationToken);

            var alterScript = await _ddlGeneratorService.GenerateAlterLandingTableAsync(
                sourceTable, fieldList, cancellationToken);

            var response = new DdlScriptResponse
            {
                LandingTableScript = landingScript,
                StagingTableScript = stagingScript,
                AlterTableScript = alterScript
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating DDL scripts for source {SourceId}, table {TableId}", sourceId, tableId);
            return StatusCode(500, new { message = "An error occurred while generating DDL scripts.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Apply DDL scripts to the Data Warehouse database.
    /// </summary>
    [HttpPost("{sourceId}/tables/{tableId}/apply-ddl")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyDdl(
        int sourceId,
        int tableId,
        [FromBody] ApplyDdlRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate request
            if (!request.ExecuteCreate && !request.ExecuteStaging && !request.ExecuteAlter)
            {
                return BadRequest(new { message = "At least one of ExecuteCreate, ExecuteStaging, or ExecuteAlter must be true." });
            }

            // Load source table and verify it belongs to the specified source
            var sourceTable = await _sourceTableRepository.GetByIdAsync(tableId, cancellationToken);
            if (sourceTable == null)
            {
                return NotFound(new { message = $"Source table with ID {tableId} not found." });
            }

            if (sourceTable.SourceId != sourceId)
            {
                return BadRequest(new { message = $"Table {tableId} does not belong to source {sourceId}." });
            }

            // Load all fields for this table
            var fields = await _sourceFieldRepository.GetBySourceTableIdAsync(tableId, cancellationToken);
            var fieldList = fields.ToList();

            if (!fieldList.Any())
            {
                return BadRequest(new { message = $"Table {tableId} has no configured fields. Cannot apply DDL." });
            }

            // Generate the required DDL scripts
            var executedScripts = new List<string>();

            if (request.ExecuteCreate)
            {
                var landingScript = await _ddlGeneratorService.GenerateCreateLandingTableAsync(
                    sourceTable, fieldList, cancellationToken);

                // Ensure schema exists before creating table
                var schemaScript = GenerateCreateSchemaScript(sourceTable);
                await _ddlGeneratorService.ExecuteDdlAsync(schemaScript, cancellationToken);

                await _ddlGeneratorService.ExecuteDdlAsync(landingScript, cancellationToken);
                executedScripts.Add("Landing table created");
                _logger.LogInformation("Created landing table for source {SourceId}, table {TableId}", sourceId, tableId);
            }

            if (request.ExecuteStaging)
            {
                var stagingScript = await _ddlGeneratorService.GenerateCreateStagingTableAsync(
                    sourceTable, fieldList, cancellationToken);

                // Ensure schema exists before creating table
                var schemaScript = GenerateCreateSchemaScript(sourceTable);
                await _ddlGeneratorService.ExecuteDdlAsync(schemaScript, cancellationToken);

                await _ddlGeneratorService.ExecuteDdlAsync(stagingScript, cancellationToken);
                executedScripts.Add("Staging table created");
                _logger.LogInformation("Created staging table for source {SourceId}, table {TableId}", sourceId, tableId);
            }

            if (request.ExecuteAlter)
            {
                var alterScript = await _ddlGeneratorService.GenerateAlterLandingTableAsync(
                    sourceTable, fieldList, cancellationToken);

                if (!string.IsNullOrWhiteSpace(alterScript))
                {
                    await _ddlGeneratorService.ExecuteDdlAsync(alterScript, cancellationToken);
                    executedScripts.Add("Landing table altered");
                    _logger.LogInformation("Altered landing table for source {SourceId}, table {TableId}", sourceId, tableId);
                }
                else
                {
                    executedScripts.Add("No ALTER needed (table up-to-date or doesn't exist)");
                }
            }

            return Ok(new
            {
                success = true,
                message = "DDL applied successfully.",
                executedOperations = executedScripts
            });
        }
        catch (Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            _logger.LogError(sqlEx, "SQL error applying DDL for source {SourceId}, table {TableId}", sourceId, tableId);
            return BadRequest(new
            {
                success = false,
                message = "SQL error occurred while applying DDL.",
                detail = sqlEx.Message,
                errorNumber = sqlEx.Number
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying DDL for source {SourceId}, table {TableId}", sourceId, tableId);
            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while applying DDL scripts.",
                detail = ex.Message
            });
        }
    }

    private string GenerateCreateSchemaScript(DwBuilder.Core.Entities.SourceTable sourceTable)
    {
        // Load source to get LandingSchema - we need to do this synchronously since we're in a helper method
        // In a real scenario, we'd pass the schema name directly or make this async
        var source = _sourceTableRepository.GetByIdAsync(sourceTable.Id, CancellationToken.None)
            .GetAwaiter().GetResult()?.Source;

        if (source == null)
        {
            throw new InvalidOperationException($"Could not load source for table {sourceTable.Id}");
        }

        var landingSchema = source.LandingSchema;

        return $@"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{landingSchema}')
BEGIN
    EXEC('CREATE SCHEMA [{landingSchema}]');
END
GO
".Trim();
    }
}
