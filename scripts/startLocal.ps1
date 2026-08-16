[CmdletBinding()]
param(
    [int]$ApiHttpPort = 5120,
    [int]$ApiHttpsPort = 7183,
    [int]$UiPort = 8080,
    [int]$StartupTimeoutSeconds = 60,
    [switch]$Compose
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

function Ensure-Azurite {
    param([Parameter(Mandatory)][string]$ApiPath)

    # Storage is the whole persistence tier, so the API cannot start without it: the
    # bootstrapper throws, the host never starts, and the only symptom here would be the
    # port timing out below. setuplocal.ps1 starts Azurite with --rm, so it does not
    # survive a reboot — starting it here is what makes this script usable day to day.
    if (Test-PortListening -Port 10002) {
        Write-Host 'Azurite already running on localhost:10002' -ForegroundColor Green
        return
    }

    Write-Host 'Azurite is not running. Starting it...' -ForegroundColor Yellow
    $composeFile = Join-Path $ApiPath 'docker-compose.yml'
    $azuriteProcess = Start-Process -FilePath 'docker' `
        -ArgumentList @('compose', '-p', 'ccdiary', '-f', $composeFile, 'up', '-d', 'azurite') `
        -WorkingDirectory $ApiPath -Wait -NoNewWindow -PassThru

    if ($azuriteProcess.ExitCode -ne 0) {
        throw "Failed to start Azurite (docker compose exited $($azuriteProcess.ExitCode)). Is Docker running?"
    }

    if (-not (Wait-ForAnyPort -Ports @(10002) -TimeoutSeconds $StartupTimeoutSeconds)) {
        throw "Azurite did not start listening on port 10002 within $StartupTimeoutSeconds seconds."
    }

    Write-Host 'Azurite is now listening on localhost:10002' -ForegroundColor Green
}

# Docker is required either way now: compose runs the API in a container, and the host-run
# path still needs Azurite behind it.
Ensure-Command -Name 'docker'

if ($Compose) {
    $ApiHttpPort = 5121
    $ApiHttpsPort = 7184
}
else {
    Ensure-Command -Name 'dotnet'
}
Ensure-Command -Name 'npm.cmd'

if (-not (Test-Path -Path $apiProject)) {
    throw "API project not found: $apiProject"
}

if (-not (Test-Path -Path $uiPath)) {
    throw "UI path not found: $uiPath"
}

Ensure-Azurite -ApiPath $apiPath

$apiRunning = (Test-PortListening -Port $ApiHttpPort) -or (Test-PortListening -Port $ApiHttpsPort)
if ($apiRunning) {
    Write-Host "API already running on localhost:$ApiHttpPort or localhost:$ApiHttpsPort" -ForegroundColor Green
}
elseif ($Compose) {
    Write-Host 'API is not running. Starting API container via docker compose...' -ForegroundColor Yellow
    $composeProcess = Start-Process -FilePath 'docker' -ArgumentList @('compose', '-p', 'ccdiary', 'up', '-d', 'ccdiaryapi') -WorkingDirectory $apiPath -Wait -NoNewWindow -PassThru
    if ($composeProcess.ExitCode -ne 0) {
        throw "docker compose exited with code $($composeProcess.ExitCode). Check for missing .env values or image build failures."
    }
    Write-Host "API container started. Waiting for port..." -ForegroundColor Cyan

    if (-not (Wait-ForAnyPort -Ports @($ApiHttpPort, $ApiHttpsPort) -TimeoutSeconds $StartupTimeoutSeconds)) {
        throw "API container did not start listening on ports $ApiHttpPort/$ApiHttpsPort within $StartupTimeoutSeconds seconds."
    }

    Write-Host "API is now listening on localhost:$ApiHttpPort or localhost:$ApiHttpsPort" -ForegroundColor Green
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
    $npmScript = if ($Compose) { 'devcompose' } else { 'dev' }
    $uiProcess = Start-Process -FilePath 'npm.cmd' -ArgumentList @('run', $npmScript) -WorkingDirectory $uiPath -PassThru
    Write-Host "UI process started (PID $($uiProcess.Id)). Waiting for port..." -ForegroundColor Cyan

    if (-not (Wait-ForAnyPort -Ports @($UiPort) -TimeoutSeconds $StartupTimeoutSeconds)) {
        throw "UI did not start listening on port $UiPort within $StartupTimeoutSeconds seconds."
    }

    Write-Host "UI is now listening on localhost:$UiPort" -ForegroundColor Green
}

Write-Host 'Local API and UI are ready.' -ForegroundColor Green

# ── Seed test data (always re-imports to pick up changes) ──────────────────────
$apiBase = "https://localhost:$ApiHttpsPort"
$dataFile = Join-Path $repoRoot 'data/test_data.json'

if (-not (Test-Path $dataFile)) {
    Write-Host "Seed data file not found: $dataFile — skipping." -ForegroundColor Yellow
}
else {
    Write-Host 'Seeding test data...' -ForegroundColor Cyan
    try {
        $response = Invoke-RestMethod `
            -Uri "$apiBase/api/v1/DiaryArchive/Import" `
            -Method Post `
            -Headers @{ 'Content-Type' = 'application/json' } `
            -Body (Get-Content $dataFile -Raw) `
            -SkipCertificateCheck
        Write-Host "Test data seeded. DiaryId=$($response.diaryId)" -ForegroundColor Green
    }
    catch {
        Write-Host "Seed failed: $_" -ForegroundColor Red
    }
}
