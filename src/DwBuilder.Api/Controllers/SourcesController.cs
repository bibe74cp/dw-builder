using DwBuilder.Core.DTOs.Sources;
using DwBuilder.Core.DTOs.SourceSchema;
using DwBuilder.Core.Entities;
using DwBuilder.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DwBuilder.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SourcesController : ControllerBase
{
    private readonly ISourceRepository _sourceRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly ISourceConnectionService _sourceConnectionService;
    private readonly ILogger<SourcesController> _logger;

    public SourcesController(
        ISourceRepository sourceRepository,
        IEncryptionService encryptionService,
        ISourceConnectionService sourceConnectionService,
        ILogger<SourcesController> logger)
    {
        _sourceRepository = sourceRepository;
        _encryptionService = encryptionService;
        _sourceConnectionService = sourceConnectionService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SourceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SourceDto>>> GetAll(CancellationToken cancellationToken)
    {
        var sources = await _sourceRepository.GetAllActiveAsync(cancellationToken);
        return Ok(sources.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SourceDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var source = await _sourceRepository.GetByIdAsync(id, cancellationToken);
        if (source is null || !source.IsActive)
            return NotFound();

        return Ok(ToDto(source));
    }

    [HttpPost]
    [ProducesResponseType(typeof(SourceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SourceDto>> Create(
        [FromBody] CreateSourceRequest request,
        CancellationToken cancellationToken)
    {
        var source = new Source
        {
            Name = request.Name,
            ServerName = request.ServerName,
            InstanceName = request.InstanceName,
            DatabaseName = request.DatabaseName,
            LandingSchema = request.LandingSchema,
            ConnectionUser = request.ConnectionUser,
            ConnectionPasswordEncrypted = !string.IsNullOrEmpty(request.ConnectionPassword)
                ? _encryptionService.Encrypt(request.ConnectionPassword)
                : null,
            IsActive = true
        };

        var created = await _sourceRepository.CreateAsync(source, cancellationToken);
        _logger.LogInformation("Created source {SourceId} - {SourceName}", created.Id, created.Name);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SourceDto>> Update(
        int id,
        [FromBody] UpdateSourceRequest request,
        CancellationToken cancellationToken)
    {
        var source = await _sourceRepository.GetByIdAsync(id, cancellationToken);
        if (source is null || !source.IsActive)
            return NotFound();

        source.Name = request.Name;
        source.ServerName = request.ServerName;
        source.InstanceName = request.InstanceName;
        source.DatabaseName = request.DatabaseName;
        source.LandingSchema = request.LandingSchema;
        source.ConnectionUser = request.ConnectionUser;

        if (!string.IsNullOrEmpty(request.ConnectionPassword))
            source.ConnectionPasswordEncrypted = _encryptionService.Encrypt(request.ConnectionPassword);

        var updated = await _sourceRepository.UpdateAsync(source, cancellationToken);
        _logger.LogInformation("Updated source {SourceId} - {SourceName}", updated.Id, updated.Name);

        return Ok(ToDto(updated));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _sourceRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound();

        _logger.LogInformation("Soft-deleted source {SourceId}", id);
        return NoContent();
    }

    /// <summary>
    /// Tests the connection to a source database.
    /// </summary>
    [HttpPost("{id:int}/test-connection")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestConnection(int id, CancellationToken cancellationToken)
    {
        var source = await _sourceRepository.GetByIdAsync(id, cancellationToken);
        if (source is null || !source.IsActive)
            return NotFound();

        try
        {
            await _sourceConnectionService.TestConnectionAsync(source, cancellationToken);
            _logger.LogInformation("Connection test successful for source {SourceId}", id);
            return Ok(new { success = true, message = "Connection successful" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Connection test failed for source {SourceId}", id);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Gets available tables from the source database.
    /// </summary>
    [HttpGet("{id:int}/available-tables")]
    [ProducesResponseType(typeof(IEnumerable<SourceTableInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<SourceTableInfo>>> GetAvailableTables(
        int id, 
        CancellationToken cancellationToken)
    {
        var source = await _sourceRepository.GetByIdAsync(id, cancellationToken);
        if (source is null || !source.IsActive)
            return NotFound();

        try
        {
            var tables = await _sourceConnectionService.GetAvailableTablesAsync(source, cancellationToken);
            return Ok(tables);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to retrieve tables from source {SourceId}", id);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private static SourceDto ToDto(Source source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        ServerName = source.ServerName,
        InstanceName = source.InstanceName,
        DatabaseName = source.DatabaseName,
        LandingSchema = source.LandingSchema,
        ConnectionUser = source.ConnectionUser,
        HasPassword = !string.IsNullOrEmpty(source.ConnectionPasswordEncrypted),
        IsActive = source.IsActive,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt
    };
}
