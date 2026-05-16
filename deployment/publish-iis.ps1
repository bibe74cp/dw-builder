# DW-Builder IIS Publish Script
# Publishes the API to a folder suitable for IIS deployment

Write-Host "DW-Builder - IIS Publish Script" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

$ProjectPath = "src\DwBuilder.Api\DwBuilder.Api.csproj"
$PublishProfile = "IIS"

# Check if project file exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "ERROR: Project file not found: $ProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Publishing DW-Builder API for IIS..." -ForegroundColor Yellow
Write-Host "Project: $ProjectPath" -ForegroundColor Cyan
Write-Host "Profile: $PublishProfile" -ForegroundColor Cyan
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

$PublishDir = "src\DwBuilder.Api\bin\Release\net10.0\publish\iis"

Write-Host ""
Write-Host "✓ Publish completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Published files location:" -ForegroundColor Yellow
Write-Host $PublishDir -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Copy published files to IIS server (e.g., C:\inetpub\wwwroot\dwbuilder)" -ForegroundColor White
Write-Host "2. Create IIS Application Pool (.NET CLR Version: No Managed Code)" -ForegroundColor White
Write-Host "3. Create IIS Website/Application pointing to published folder" -ForegroundColor White
Write-Host "4. Copy deployment\iis\web.config.template to web.config and configure" -ForegroundColor White
Write-Host "5. Set environment variables or update appsettings.Production.json" -ForegroundColor White
Write-Host "6. Ensure App Pool identity has permissions on logs folder" -ForegroundColor White
Write-Host ""

# Open publish folder
Write-Host "Opening publish folder..." -ForegroundColor Yellow
Start-Process $PublishDir

Write-Host ""
Write-Host "Done! Press any key to exit..." -ForegroundColor Green
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
