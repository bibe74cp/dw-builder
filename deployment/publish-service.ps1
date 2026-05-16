# DW-Builder Windows Service Publish Script
# Publishes the API as a self-contained executable for Windows Service deployment

Write-Host "DW-Builder - Windows Service Publish Script" -ForegroundColor Green
Write-Host "===========================================" -ForegroundColor Green
Write-Host ""

$ProjectPath = "src\DwBuilder.Api\DwBuilder.Api.csproj"
$PublishProfile = "WindowsService"

# Check if project file exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "ERROR: Project file not found: $ProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Publishing DW-Builder API for Windows Service..." -ForegroundColor Yellow
Write-Host "Project: $ProjectPath" -ForegroundColor Cyan
Write-Host "Profile: $PublishProfile" -ForegroundColor Cyan
Write-Host "Output: Self-contained single executable" -ForegroundColor Cyan
Write-Host ""

# Publish
dotnet publish $ProjectPath `
    -c Release `
    -p:PublishProfile=$PublishProfile `
    --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Publish failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

$PublishDir = "src\DwBuilder.Api\bin\Release\net10.0\publish\service"

Write-Host ""
Write-Host "✓ Publish completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Published files location:" -ForegroundColor Yellow
Write-Host $PublishDir -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Copy all files to C:\Services\DwBuilder\ on target server" -ForegroundColor White
Write-Host "2. Ensure appsettings.Production.json is configured with secrets" -ForegroundColor White
Write-Host "3. Run .\deployment\install-service.ps1 as Administrator" -ForegroundColor White
Write-Host "4. Configure environment variables in Windows Registry (see script)" -ForegroundColor White
Write-Host "5. Start the service: Start-Service -Name DwBuilderApi" -ForegroundColor White
Write-Host ""

# Open publish folder
Write-Host "Opening publish folder..." -ForegroundColor Yellow
Start-Process $PublishDir

Write-Host ""
Write-Host "Done! Press any key to exit..." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
