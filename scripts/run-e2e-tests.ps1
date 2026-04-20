[CmdletBinding()]
param(
    [switch]$SkipEnsureRunning,
    [string]$BaseUrl,
    [string]$ApiUrl
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot  = Split-Path -Parent $PSScriptRoot
$uiPath    = Join-Path $repoRoot 'src/ui'
$reportXml = Join-Path $uiPath 'playwright-report/junit.xml'

# ── Optionally ensure API + UI are running and data is seeded ──────────────────
if (-not $SkipEnsureRunning) {
    & "$PSScriptRoot/startLocal.ps1"
}

# ── Run Playwright ─────────────────────────────────────────────────────────────
Write-Host ''
Write-Host 'Running E2E tests...' -ForegroundColor Cyan

$env:PLAYWRIGHT_BASE_URL = if ($BaseUrl) { $BaseUrl } else { $env:PLAYWRIGHT_BASE_URL }
$env:PLAYWRIGHT_API_URL  = if ($ApiUrl)  { $ApiUrl  } else { $env:PLAYWRIGHT_API_URL  }

$playwrightExit = 0
Push-Location $uiPath
try {
    npm run test:e2e
    $playwrightExit = $LASTEXITCODE
}
finally {
    Pop-Location
}

# ── Parse JUnit XML ────────────────────────────────────────────────────────────
if (-not (Test-Path $reportXml)) {
    Write-Host "No JUnit report found at: $reportXml" -ForegroundColor Red
    exit 1
}

[xml]$xml = Get-Content $reportXml -Raw

$totalTests  = 0
$totalFailed = 0
$totalSkipped = 0
$failedTests  = [System.Collections.Generic.List[pscustomobject]]::new()

foreach ($suite in $xml.testsuites.testsuite) {
    foreach ($tc in $suite.testcase) {
        $totalTests++
        $failure = $tc.SelectSingleNode('failure')
        $skipped = $tc.SelectSingleNode('skipped')
        if ($skipped) {
            $totalSkipped++
        }
        elseif ($failure) {
            $totalFailed++
            $failedTests.Add([pscustomobject]@{
                Suite   = $suite.name
                Test    = $tc.name
                Message = ($failure.message -replace '\r?\n.*', '').Trim()
            })
        }
    }
}

$totalPassed = $totalTests - $totalFailed - $totalSkipped

# ── Print summary ──────────────────────────────────────────────────────────────
Write-Host ''
Write-Host ('─' * 60)
Write-Host 'E2E Test Summary'
Write-Host ('─' * 60)
Write-Host ("{0,-10} {1}" -f 'Total:',   $totalTests)
Write-Host ("{0,-10} {1}" -f 'Passed:',  $totalPassed)  -ForegroundColor Green
if ($totalSkipped -gt 0) {
    Write-Host ("{0,-10} {1}" -f 'Skipped:', $totalSkipped) -ForegroundColor Yellow
}
if ($totalFailed -gt 0) {
    Write-Host ("{0,-10} {1}" -f 'Failed:',  $totalFailed)  -ForegroundColor Red
}
Write-Host ('─' * 60)

if ($totalFailed -gt 0) {
    Write-Host ''
    Write-Host 'Failed tests:' -ForegroundColor Red
    foreach ($f in $failedTests) {
        Write-Host "  ✗ $($f.Suite) › $($f.Test)" -ForegroundColor Red
        if ($f.Message) {
            Write-Host "      $($f.Message)" -ForegroundColor DarkRed
        }
    }
    Write-Host ''
    Write-Host 'FAILED' -ForegroundColor Red
}
else {
    Write-Host ''
    Write-Host 'PASSED' -ForegroundColor Green
}

Write-Host ''
Write-Host "Full report: $uiPath\playwright-report\index.html"

exit $playwrightExit
