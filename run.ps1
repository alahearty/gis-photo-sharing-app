#requires -Version 5.1
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Stop-WithError {
    param([string]$Message)
    Write-Host ""
    Write-Host "ERROR: $Message" -ForegroundColor Red
    exit 1
}

function Require-Command {
    param([string]$Name, [string]$Hint)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Stop-WithError "$Name was not found. $Hint"
    }
}

function Start-AppWindow {
    param([string]$Title, [string]$WorkingDirectory, [string]$Command)

    $safeTitle = $Title.Replace("'", "''")
    $safeDirectory = $WorkingDirectory.Replace("'", "''")
    $script = @'
$Host.UI.RawUI.WindowTitle = '__TITLE__'
Set-Location -LiteralPath '__DIRECTORY__'
__COMMAND__
'@
    $script = $script.Replace('__TITLE__', $safeTitle).Replace('__DIRECTORY__', $safeDirectory).Replace('__COMMAND__', $Command)
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
    Start-Process -FilePath 'powershell.exe' -ArgumentList @(
        '-NoExit',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-EncodedCommand', $encoded
    ) | Out-Null
}

Write-Host "GIS PhotoShare launcher" -ForegroundColor Green
Write-Host "Repository: $repoRoot"

Require-Command 'dotnet' 'Install the .NET 8 SDK or newer and reopen this window.'
Require-Command 'node' 'Install Node.js 18 or newer and reopen this window.'
Require-Command 'npm' 'Install npm and reopen this window.'

$clientDirectory = Join-Path $repoRoot 'src\WebUI\clientapp'
if (-not (Test-Path (Join-Path $clientDirectory 'node_modules'))) {
    Write-Step 'Installing frontend dependencies'
    Push-Location $clientDirectory
    try {
        & npm ci
        if ($LASTEXITCODE -ne 0) { Stop-WithError 'npm ci failed.' }
    }
    finally {
        Pop-Location
    }
}

Write-Host ""
Write-Host "The Vue frontend is built automatically and served by the ASP.NET Core WebUI process." -ForegroundColor Yellow
Write-Host "Default local database: SQLite. No external database server is required."
Write-Host "Demo login: administrator@localhost / Administrator1!"

Write-Step 'Building and starting GIS PhotoShare on http://localhost:5000'
Start-AppWindow -Title 'GIS PhotoShare (http://localhost:5000)' -WorkingDirectory $repoRoot -Command @'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project 'src\WebUI' --launch-profile WebUI
'@

Write-Host ""
Write-Host "Application: http://localhost:5000" -ForegroundColor Yellow
Write-Host "HTTPS:       https://localhost:5001"
Write-Host "The first build runs npm and can take a minute. The WebUI profile opens the browser automatically."
