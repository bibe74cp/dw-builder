# DW-Builder Windows Service Installation Script
# Installs DW-Builder API as a Windows Service

#Requires -RunAsAdministrator

Write-Host "DW-Builder - Windows Service Installation" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

# Configuration
$ServiceName = "DwBuilderApi"
$ServiceDisplayName = "DW-Builder API Service"
$ServiceDescription = "Data Warehouse Builder REST API"
$ServicePath = "C:\Services\DwBuilder"
$ExePath = Join-Path $ServicePath "DwBuilder.Api.exe"

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

if ($existingService) {
    Write-Host "Service '$ServiceName' already exists." -ForegroundColor Yellow
    $response = Read-Host "Do you want to reinstall? (y/n)"
    
    if ($response -ne 'y') {
        Write-Host "Installation cancelled." -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "Stopping existing service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    Write-Host "Removing existing service..." -ForegroundColor Yellow
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

# Check if exe exists
if (-not (Test-Path $ExePath)) {
    Write-Host "ERROR: Service executable not found at: $ExePath" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please ensure you have:" -ForegroundColor Yellow
    Write-Host "1. Published the service using: .\deployment\publish-service.ps1" -ForegroundColor White
    Write-Host "2. Copied published files to: $ServicePath" -ForegroundColor White
    exit 1
}

# Create service
Write-Host "Creating Windows Service..." -ForegroundColor Yellow
Write-Host "Name: $ServiceName" -ForegroundColor Cyan
Write-Host "Display Name: $ServiceDisplayName" -ForegroundColor Cyan
Write-Host "Binary Path: $ExePath" -ForegroundColor Cyan
Write-Host ""

sc.exe create $ServiceName `
    binPath= "`"$ExePath`"" `
    start= auto `
    DisplayName= "$ServiceDisplayName"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to create service!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Set service description
sc.exe description $ServiceName "$ServiceDescription"

# Configure service recovery options (restart on failure)
Write-Host "Configuring service recovery options..." -ForegroundColor Yellow
sc.exe failure $ServiceName reset= 86400 actions= restart/60000/restart/60000/restart/60000

# Set service to run as Local System (or configure specific user)
Write-Host ""
Write-Host "Service created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "IMPORTANT: Configure environment variables before starting:" -ForegroundColor Yellow
Write-Host "1. Open Registry Editor (regedit)" -ForegroundColor White
Write-Host "2. Navigate to: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\$ServiceName" -ForegroundColor White
Write-Host "3. Create Multi-String Value named 'Environment'" -ForegroundColor White
Write-Host "4. Add environment variables (one per line):" -ForegroundColor White
Write-Host "   DB_CONNECTION_STRING=Server=...;Database=DwBuilderDW;..." -ForegroundColor Gray
Write-Host "   JWT_KEY=your_jwt_key" -ForegroundColor Gray
Write-Host "   ENCRYPTION_KEY=your_encryption_key" -ForegroundColor Gray
Write-Host "   ASPNETCORE_ENVIRONMENT=Production" -ForegroundColor Gray
Write-Host ""
Write-Host "OR use appsettings.Production.json with secrets configured" -ForegroundColor White
Write-Host ""

$startNow = Read-Host "Do you want to start the service now? (y/n)"

if ($startNow -eq 'y') {
    Write-Host ""
    Write-Host "Starting service..." -ForegroundColor Yellow
    Start-Service -Name $ServiceName
    
    Start-Sleep -Seconds 3
    
    $service = Get-Service -Name $ServiceName
    if ($service.Status -eq 'Running') {
        Write-Host "✓ Service started successfully!" -ForegroundColor Green
    } else {
        Write-Host "✗ Service failed to start. Check Event Viewer for errors." -ForegroundColor Red
        Write-Host "Common issues:" -ForegroundColor Yellow
        Write-Host "- Missing environment variables" -ForegroundColor White
        Write-Host "- Database connection failure" -ForegroundColor White
        Write-Host "- Port already in use" -ForegroundColor White
    }
} else {
    Write-Host ""
    Write-Host "Service installed but not started." -ForegroundColor Yellow
    Write-Host "To start manually: Start-Service -Name $ServiceName" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Service Management Commands:" -ForegroundColor Yellow
Write-Host "  Start:   Start-Service -Name $ServiceName" -ForegroundColor Cyan
Write-Host "  Stop:    Stop-Service -Name $ServiceName" -ForegroundColor Cyan
Write-Host "  Status:  Get-Service -Name $ServiceName" -ForegroundColor Cyan
Write-Host "  Remove:  sc.exe delete $ServiceName" -ForegroundColor Cyan
Write-Host ""
Write-Host "Done!" -ForegroundColor Green
