namespace DwBuilder.Core.Interfaces;

/// <summary>
/// Service for generating BIML master template files.
/// The generated .biml file contains BimlScript (C# embedded) that reads metadata from _meta schema
/// and dynamically generates SSIS packages for ETL operations.
/// </summary>
public interface IBimlGenerator
{
    /// <summary>
    /// Generates a complete BIML master template file.
    /// </summary>
    /// <param name="dwConnectionString">Connection string to the Data Warehouse database containing _meta schema.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A string containing the complete BIML XML template.</returns>
    Task<string> GenerateBimlAsync(string dwConnectionString, CancellationToken cancellationToken = default);
}
