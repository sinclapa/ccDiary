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

# ── Seed test data if not already present ──────────────────────────────────────
$apiBase = "https://localhost:$ApiHttpsPort"
$dataFile = Join-Path $repoRoot 'data/test_data.json'

if (-not (Test-Path $dataFile)) {
    Write-Host "Seed data file not found: $dataFile — skipping." -ForegroundColor Yellow
}
else {
    try {
        $diaries = Invoke-RestMethod -Uri "$apiBase/api/v1/Diary/Get" -SkipCertificateCheck
        $alreadySeeded = $diaries | Where-Object { $_.title -eq 'Integration Test Diary' }
    }
    catch {
        $alreadySeeded = $null
    }

    if ($alreadySeeded) {
        Write-Host 'Test data already seeded — skipping import.' -ForegroundColor Green
    }
    else {
        Write-Host 'Seeding test data...' -ForegroundColor Cyan

        # Read Entra config from the API .env file written by setuplocal.ps1
        $envFile = Join-Path $repoRoot 'src/api/.env'
        $appIdUri = $null
        $clientId = $null
        $tenantId = $null
        if (Test-Path $envFile) {
            $envLines = Get-Content $envFile
            foreach ($line in $envLines) {
                if ($line -match '^Entra__ApplicationIdUri=(.+)') { $appIdUri = $Matches[1] }
                if ($line -match '^Entra__ClientId=(.+)')         { $clientId = $Matches[1] }
                if ($line -match '^Entra__TenantId=(.+)')         { $tenantId = $Matches[1] }
            }
        }

        if (-not $appIdUri -or -not $clientId -or -not $tenantId) {
            Write-Host 'Missing Entra config in src/api/.env — skipping seed.' -ForegroundColor Yellow
        }
        else {
            $scope = "${appIdUri}/Diary.Update"

            # Use the app's own client ID with MSAL device code flow.
            # Azure CLI cannot request tokens for this API, but the app's own public client can.
            $tokenFile = [System.IO.Path]::GetTempFileName()
            $pythonScript = @"
import sys, msal
token_file = sys.argv[1]
app = msal.PublicClientApplication('$clientId', authority='https://login.microsoftonline.com/$tenantId')
accounts = app.get_accounts()
result = app.acquire_token_silent(['$scope'], account=accounts[0]) if accounts else None
if not result:
    flow = app.initiate_device_flow(scopes=['$scope'])
    print(flow['message'], flush=True)
    result = app.acquire_token_by_device_flow(flow)
token = ''
if result and 'access_token' in result:
    token = result['access_token']
else:
    err = result.get('error_description') or result.get('error') if result else 'no result'
    print('MSAL error: ' + str(err), flush=True)
with open(token_file, 'w') as f:
    f.write(token)
"@
            python -c $pythonScript $tokenFile
            $rawToken = Get-Content $tokenFile -Raw -ErrorAction SilentlyContinue
            Remove-Item $tokenFile -Force -ErrorAction SilentlyContinue
            $token = if ($rawToken) { $rawToken.Trim() } else { '' }
            if (-not $token) {
                Write-Host 'Could not acquire access token — skipping seed.' -ForegroundColor Yellow
            }
            else {
                try {
                    $response = Invoke-RestMethod `
                        -Uri "$apiBase/api/v1/DiaryArchive/Import" `
                        -Method Post `
                        -Headers @{ Authorization = "Bearer $token"; 'Content-Type' = 'application/json' } `
                        -Body (Get-Content $dataFile -Raw) `
                        -SkipCertificateCheck
                    Write-Host "Test data seeded. DiaryId=$($response.diaryId)" -ForegroundColor Green
                }
                catch {
                    Write-Host "Seed failed: $_" -ForegroundColor Red
                }
            }
        }
    }
}
