[CmdletBinding()]
param(
    [int]$ApiHttpPort = 5120,
    [int]$ApiHttpsPort = 7183,
    [int]$UiPort = 8080,
    [int]$StartupTimeoutSeconds = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiPath = Join-Path $repoRoot 'src/api'
$apiProject = Join-Path $apiPath 'ccDiaryApi/ccDiaryApi.csproj'
$uiPath = Join-Path $repoRoot 'src/ui'

function Test-PortListening {
    param([Parameter(Mandatory)][int]$Port)

    $listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -First 1

    return $null -ne $listener
}

function Wait-ForAnyPort {
    param(
        [Parameter(Mandatory)][int[]]$Ports,
        [Parameter(Mandatory)][int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        foreach ($port in $Ports) {
            if (Test-PortListening -Port $port) {
                return $true
            }
        }

        Start-Sleep -Seconds 1
    }

    return $false
}

function Ensure-Command {
    param([Parameter(Mandatory)][string]$Name)

    if (-not (Get-Command -Name $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' is not available on PATH."
    }
}

Ensure-Command -Name 'dotnet'
Ensure-Command -Name 'npm.cmd'

if (-not (Test-Path -Path $apiProject)) {
    throw "API project not found: $apiProject"
}

if (-not (Test-Path -Path $uiPath)) {
    throw "UI path not found: $uiPath"
}

$apiRunning = (Test-PortListening -Port $ApiHttpPort) -or (Test-PortListening -Port $ApiHttpsPort)
if ($apiRunning) {
    Write-Host "API already running on localhost:$ApiHttpPort or localhost:$ApiHttpsPort" -ForegroundColor Green
}
else {
    Write-Host 'API is not running. Starting API...' -ForegroundColor Yellow
    $apiProcess = Start-Process -FilePath 'dotnet' -ArgumentList @('run', '--project', $apiProject, '--launch-profile', 'https') -WorkingDirectory $apiPath -PassThru
    Write-Host "API process started (PID $($apiProcess.Id)). Waiting for port..." -ForegroundColor Cyan

    if (-not (Wait-ForAnyPort -Ports @($ApiHttpPort, $ApiHttpsPort) -TimeoutSeconds $StartupTimeoutSeconds)) {
        throw "API did not start listening on ports $ApiHttpPort/$ApiHttpsPort within $StartupTimeoutSeconds seconds."
    }

    Write-Host "API is now listening on localhost:$ApiHttpPort or localhost:$ApiHttpsPort" -ForegroundColor Green
}

$uiRunning = Test-PortListening -Port $UiPort
if ($uiRunning) {
    Write-Host "UI already running on localhost:$UiPort" -ForegroundColor Green
}
else {
    Write-Host 'UI is not running. Starting UI...' -ForegroundColor Yellow
    $uiProcess = Start-Process -FilePath 'npm.cmd' -ArgumentList @('run', 'dev') -WorkingDirectory $uiPath -PassThru
    Write-Host "UI process started (PID $($uiProcess.Id)). Waiting for port..." -ForegroundColor Cyan

    if (-not (Wait-ForAnyPort -Ports @($UiPort) -TimeoutSeconds $StartupTimeoutSeconds)) {
        throw "UI did not start listening on port $UiPort within $StartupTimeoutSeconds seconds."
    }

    Write-Host "UI is now listening on localhost:$UiPort" -ForegroundColor Green
}

Write-Host 'Local API and UI are ready.' -ForegroundColor Green
