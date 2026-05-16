using DwBuilder.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DwBuilder.Api.Controllers;

/// <summary>
/// API endpoints for BIML template generation.
/// </summary>
[ApiController]
[Route("api/v1/biml")]
[Authorize]
public class BimlController : ControllerBase
{
    private readonly IBimlGenerator _bimlGenerator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BimlController> _logger;
    
    public BimlController(
        IBimlGenerator bimlGenerator,
        IConfiguration configuration,
        ILogger<BimlController> logger)
    {
        _bimlGenerator = bimlGenerator;
        _configuration = configuration;
        _logger = logger;
    }
    
    /// <summary>
    /// Generates the complete BIML master template file from _meta schema.
    /// </summary>
    /// <returns>A .biml XML file ready to be compiled with BimlExpress or BimlStudio.</returns>
    /// <response code="200">Returns the BIML XML content as application/xml.</response>
    /// <response code="500">If an error occurs during generation.</response>
    [HttpGet]
    [Produces("application/xml")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateBiml(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("BIML generation requested by user");
            
            // Get DW connection string from configuration
            var dwConnectionString = _configuration.GetConnectionString("DwBuilder");
            
            if (string.IsNullOrWhiteSpace(dwConnectionString))
            {
                _logger.LogError("DwBuilder connection string not configured");
                return Problem(
                    title: "Configuration Error",
                    detail: "Data Warehouse connection string is not configured.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            
            // Generate BIML content
            var bimlContent = await _bimlGenerator.GenerateBimlAsync(dwConnectionString, cancellationToken);
            
            // Set response headers for file download
            var fileName = $"DwBuilder_Master_{DateTime.UtcNow:yyyyMMddHHmmss}.biml";
            Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            
            _logger.LogInformation("BIML generation completed successfully, file: {FileName}", fileName);
            
            return Content(bimlContent, "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating BIML template");
            return Problem(
                title: "BIML Generation Error",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
