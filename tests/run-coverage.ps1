# DW-Builder Test Coverage Script
# This script runs all tests with code coverage collection and generates an HTML report.

Write-Host "DW-Builder - Test Coverage Report Generator" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

# Clean previous test results
Write-Host "Cleaning previous test results..." -ForegroundColor Yellow
if (Test-Path ".\TestResults") {
    Remove-Item -Path ".\TestResults" -Recurse -Force
}

# Run tests with coverage collection
Write-Host "Running tests with coverage collection..." -ForegroundColor Yellow
dotnet test tests\DwBuilder.Tests\DwBuilder.Tests.csproj `
    --collect:"XPlat Code Coverage" `
    --results-directory .\TestResults `
    --logger "console;verbosity=normal"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed! Aborting coverage report generation." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Test execution completed successfully!" -ForegroundColor Green
Write-Host ""

# Check if reportgenerator tool is installed
Write-Host "Checking for reportgenerator tool..." -ForegroundColor Yellow
$reportGenInstalled = dotnet tool list -g | Select-String "dotnet-reportgenerator-globaltool"

if (-not $reportGenInstalled) {
    Write-Host "Installing dotnet-reportgenerator-globaltool..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
}

# Find coverage files
$coverageFiles = Get-ChildItem -Path ".\TestResults" -Filter "coverage.cobertura.xml" -Recurse

if ($coverageFiles.Count -eq 0) {
    Write-Host "No coverage files found!" -ForegroundColor Red
    exit 1
}

Write-Host "Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor Green

# Generate HTML report
Write-Host "Generating HTML coverage report..." -ForegroundColor Yellow
$reportPath = ".\TestResults\CoverageReport"

reportgenerator `
    -reports:".\TestResults\**\coverage.cobertura.xml" `
    -targetdir:$reportPath `
    -reporttypes:"Html;HtmlSummary;Badges;TextSummary" `
    -historydir:".\TestResults\CoverageHistory"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Report generation failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Coverage report generated successfully!" -ForegroundColor Green
Write-Host "Report location: $reportPath\index.html" -ForegroundColor Cyan
Write-Host ""

# Display summary
$summaryFile = Join-Path $reportPath "Summary.txt"
if (Test-Path $summaryFile) {
    Write-Host "Coverage Summary:" -ForegroundColor Yellow
    Write-Host "=================" -ForegroundColor Yellow
    Get-Content $summaryFile | Write-Host
}

Write-Host ""
Write-Host "Opening report in browser..." -ForegroundColor Yellow
$indexPath = Join-Path $reportPath "index.html"
Start-Process $indexPath

Write-Host ""
Write-Host "Done! Press any key to exit..." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
