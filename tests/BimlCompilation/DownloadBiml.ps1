# ============================================================================
# DOWNLOAD BIML FILE FROM DW-BUILDER API
# ============================================================================
# Description: Downloads MasterTemplate.biml from DW-Builder API endpoint
# Usage: .\DownloadBiml.ps1 -JwtToken "your-jwt-token" -OutputPath ".\MasterTemplate.biml"
# ============================================================================

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$JwtToken,

    [Parameter(Mandatory = $false)]
    [string]$ApiBaseUrl = "http://localhost:5000",

    [Parameter(Mandatory = $false)]
    [string]$OutputPath = ".\MasterTemplate.biml"
)

# ============================================================================
# CONFIGURATION
# ============================================================================

$BimlEndpoint = "$ApiBaseUrl/api/v1/biml"

# ============================================================================
# DOWNLOAD BIML FILE
# ============================================================================

try {
    Write-Host "Downloading BIML file from API..." -ForegroundColor Cyan
    Write-Host "Endpoint: $BimlEndpoint" -ForegroundColor Gray

    # Prepare headers
    $headers = @{
        "Authorization" = "Bearer $JwtToken"
        "Accept" = "application/xml, text/xml, */*"
    }

    # Make HTTP request
    $response = Invoke-WebRequest -Uri $BimlEndpoint `
                                  -Method GET `
                                  -Headers $headers `
                                  -UseBasicParsing `
                                  -ErrorAction Stop

    # Check response status
    if ($response.StatusCode -ne 200) {
        throw "API returned status code $($response.StatusCode): $($response.StatusDescription)"
    }

    # Save response content to file
    $response.Content | Out-File -FilePath $OutputPath -Encoding UTF8 -Force

    Write-Host "✅ BIML file downloaded successfully!" -ForegroundColor Green
    Write-Host "File saved to: $OutputPath" -ForegroundColor Green
    Write-Host "File size: $([math]::Round((Get-Item $OutputPath).Length / 1KB, 2)) KB" -ForegroundColor Gray

    # Display first 10 lines as preview
    Write-Host "`nPreview (first 10 lines):" -ForegroundColor Yellow
    Get-Content $OutputPath -TotalCount 10 | ForEach-Object { Write-Host $_ -ForegroundColor DarkGray }

    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Open Visual Studio 2022" -ForegroundColor White
    Write-Host "2. Create new Integration Services Project" -ForegroundColor White
    Write-Host "3. Add $OutputPath to project" -ForegroundColor White
    Write-Host "4. Right-click → 'Expand Biml File' (BimlExpress)" -ForegroundColor White

} catch {
    Write-Host "❌ Error downloading BIML file:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red

    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "HTTP Status Code: $statusCode" -ForegroundColor Yellow

        if ($statusCode -eq 401) {
            Write-Host "Authentication failed. Check your JWT token." -ForegroundColor Yellow
        } elseif ($statusCode -eq 404) {
            Write-Host "API endpoint not found. Check API base URL." -ForegroundColor Yellow
        } elseif ($statusCode -eq 500) {
            Write-Host "Server error. Check API logs." -ForegroundColor Yellow
        }
    }

    exit 1
}

# ============================================================================
# SCRIPT COMPLETED
# ============================================================================
