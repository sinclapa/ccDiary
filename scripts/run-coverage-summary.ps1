[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$NoRestore,
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiPath = Join-Path $repoRoot 'src/api'
$apiSolution = Join-Path $apiPath 'ccDiary.sln'
$apiRunSettings = Join-Path $apiPath 'ccDiary.runsettings'
$apiResultsDir = Join-Path $apiPath 'TestResults/coverage-api'
$apiCoverageFile = Join-Path $repoRoot 'src/api/TestResults/coverage-api.cobertura.xml'
$uiPath = Join-Path $repoRoot 'src/ui'
$uiCoverageDir = Join-Path $uiPath 'coverage'
$scriptLogDir = Join-Path $repoRoot 'TestResults/coverage-summary'

function Get-CoberturaSummary {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [Parameter(Mandatory)]
        [string]$ProjectName
    )

    if (-not (Test-Path -Path $Path)) {
        throw "Coverage file not found for ${ProjectName}: $Path"
    }

    [xml]$xml = Get-Content -Path $Path -Raw
    $coverage = $xml.SelectSingleNode('/coverage')
    if ($null -eq $coverage) {
        throw "Invalid Cobertura XML in $Path"
    }

    $lineValidText = $coverage.GetAttribute('lines-valid')
    $lineCoveredText = $coverage.GetAttribute('lines-covered')
    $branchValidText = $coverage.GetAttribute('branches-valid')
    $branchCoveredText = $coverage.GetAttribute('branches-covered')

    $lineRateFromXml = 0.0
    $branchRateFromXml = 0.0
    [void][double]::TryParse($coverage.GetAttribute('line-rate'), [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$lineRateFromXml)
    [void][double]::TryParse($coverage.GetAttribute('branch-rate'), [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$branchRateFromXml)

    $lineValid = 0
    $lineCovered = 0
    $branchValid = 0
    $branchCovered = 0

    [void][int]::TryParse($lineValidText, [ref]$lineValid)
    [void][int]::TryParse($lineCoveredText, [ref]$lineCovered)
    [void][int]::TryParse($branchValidText, [ref]$branchValid)
    [void][int]::TryParse($branchCoveredText, [ref]$branchCovered)

    $lineRate = if ($lineValid -gt 0) { $lineCovered / $lineValid } else { $lineRateFromXml }
    $branchRate = if ($branchValid -gt 0) { $branchCovered / $branchValid } else { $branchRateFromXml }

    [pscustomobject]@{
        Project = $ProjectName
        File = $Path
        LineCovered = $lineCovered
        LineValid = $lineValid
        LineRate = $lineRate
        BranchCovered = $branchCovered
        BranchValid = $branchValid
        BranchRate = $branchRate
    }
}

function Find-UiCoberturaFile {
    param([Parameter(Mandatory)][string]$CoverageDir)

    $preferred = Join-Path $CoverageDir 'cobertura-coverage.xml'
    if (Test-Path -Path $preferred) {
        return $preferred
    }

    $candidate = Get-ChildItem -Path $CoverageDir -Recurse -File -Filter '*.xml' |
        Where-Object { $_.Name -match 'cobertura' } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $candidate) {
        throw "Could not find a UI cobertura XML file under $CoverageDir"
    }

    return $candidate.FullName
}

function Get-PercentColor {
    param([double]$Rate)

    $pct = $Rate * 100
    if ($pct -lt 80) {
        return 'Red'
    }

    if ($pct -lt 90) {
        return 'Yellow'
    }

    return 'Green'
}

function Write-CoverageRow {
    param(
        [Parameter(Mandatory)]
        [string]$Project,
        [Parameter(Mandatory)]
        [double]$LineRate,
        [Parameter(Mandatory)]
        [double]$BranchRate
    )

    Write-Host ("{0,-18} " -f $Project) -NoNewline

    $lineText = "{0,8:N2}%" -f ($LineRate * 100)
    Write-Host $lineText -ForegroundColor (Get-PercentColor -Rate $LineRate) -NoNewline

    Write-Host '  ' -NoNewline

    $branchText = "{0,8:N2}%" -f ($BranchRate * 100)
    Write-Host $branchText -ForegroundColor (Get-PercentColor -Rate $BranchRate)
}

Write-Host 'Running API and UI coverage in parallel...' -ForegroundColor Cyan
New-Item -ItemType Directory -Path (Split-Path -Parent $apiCoverageFile) -Force | Out-Null
if (Test-Path -Path $apiCoverageFile) {
    Remove-Item -Path $apiCoverageFile -Force
}
if (Test-Path -Path $apiResultsDir) {
    Remove-Item -Path $apiResultsDir -Recurse -Force
}

$dotnetArgs = New-Object 'System.Collections.Generic.List[string]'
[void]$dotnetArgs.Add('test')
[void]$dotnetArgs.Add($apiSolution)
[void]$dotnetArgs.Add('-c')
[void]$dotnetArgs.Add($Configuration)
[void]$dotnetArgs.Add('--settings')
[void]$dotnetArgs.Add($apiRunSettings)
[void]$dotnetArgs.Add('--collect:"XPlat Code Coverage"')
[void]$dotnetArgs.Add('--results-directory')
[void]$dotnetArgs.Add($apiResultsDir)
if ($NoRestore) {
    [void]$dotnetArgs.Add('--no-restore')
}
if ($NoBuild) {
    [void]$dotnetArgs.Add('--no-build')
}

New-Item -ItemType Directory -Path $scriptLogDir -Force | Out-Null

$apiOutLog = Join-Path $scriptLogDir 'coverage-api.out.log'
$apiErrLog = Join-Path $scriptLogDir 'coverage-api.err.log'
$uiOutLog = Join-Path $scriptLogDir 'coverage-ui.out.log'
$uiErrLog = Join-Path $scriptLogDir 'coverage-ui.err.log'

foreach ($logPath in @($apiOutLog, $apiErrLog, $uiOutLog, $uiErrLog)) {
    if (Test-Path -Path $logPath) {
        Remove-Item -Path $logPath -Force
    }
}

$apiProcess = Start-Process -FilePath 'dotnet' -ArgumentList $dotnetArgs -WorkingDirectory $apiPath -NoNewWindow -PassThru -RedirectStandardOutput $apiOutLog -RedirectStandardError $apiErrLog
$uiProcess = Start-Process -FilePath 'npm.cmd' -ArgumentList @('run', 'coverage') -WorkingDirectory $uiPath -NoNewWindow -PassThru -RedirectStandardOutput $uiOutLog -RedirectStandardError $uiErrLog

$null = $apiProcess.WaitForExit()
$null = $uiProcess.WaitForExit()

if ($null -ne $apiProcess.ExitCode -and $apiProcess.ExitCode -ne 0) {
    Write-Host 'API coverage failed. Last output:' -ForegroundColor Red
    if (Test-Path -Path $apiOutLog) {
        Get-Content -Path $apiOutLog -Tail 20
    }

    if (Test-Path -Path $apiErrLog) {
        Get-Content -Path $apiErrLog -Tail 20
    }

    throw "API coverage run failed with exit code $($apiProcess.ExitCode)."
}

if ($null -ne $uiProcess.ExitCode -and $uiProcess.ExitCode -ne 0) {
    Write-Host 'UI coverage failed. Last output:' -ForegroundColor Red
    if (Test-Path -Path $uiOutLog) {
        Get-Content -Path $uiOutLog -Tail 20
    }

    if (Test-Path -Path $uiErrLog) {
        Get-Content -Path $uiErrLog -Tail 20
    }

    throw "UI coverage run failed with exit code $($uiProcess.ExitCode)."
}

$apiCoverageCandidate = Get-ChildItem -Path $apiResultsDir -Recurse -File -Filter 'coverage.cobertura.xml' |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -eq $apiCoverageCandidate) {
    throw "Could not find API cobertura XML file under $apiResultsDir"
}

Copy-Item -Path $apiCoverageCandidate.FullName -Destination $apiCoverageFile -Force

$uiCoverageFile = Find-UiCoberturaFile -CoverageDir $uiCoverageDir

$apiSummary = Get-CoberturaSummary -Path $apiCoverageFile -ProjectName 'API'
$uiSummary = Get-CoberturaSummary -Path $uiCoverageFile -ProjectName 'UI'

$overallLineValid = $apiSummary.LineValid + $uiSummary.LineValid
$overallLineCovered = $apiSummary.LineCovered + $uiSummary.LineCovered
$overallBranchValid = $apiSummary.BranchValid + $uiSummary.BranchValid
$overallBranchCovered = $apiSummary.BranchCovered + $uiSummary.BranchCovered

$overallLineRate = if ($overallLineValid -gt 0) { $overallLineCovered / $overallLineValid } else { 0 }
$overallBranchRate = if ($overallBranchValid -gt 0) { $overallBranchCovered / $overallBranchValid } else { 0 }

$result = @(
    $apiSummary,
    $uiSummary,
    [pscustomobject]@{
        Project = 'OVERALL (weighted)'
        File = '-'
        LineCovered = $overallLineCovered
        LineValid = $overallLineValid
        LineRate = $overallLineRate
        BranchCovered = $overallBranchCovered
        BranchValid = $overallBranchValid
        BranchRate = $overallBranchRate
    }
)

Write-Host ''
Write-Host ('{0,-18} {1,9} {2,10}' -f 'Project', 'Line %', 'Branch %')
Write-Host ('{0,-18} {1,9} {2,10}' -f ('-' * 18), ('-' * 9), ('-' * 10))

foreach ($item in $result) {
    Write-CoverageRow -Project $item.Project -LineRate $item.LineRate -BranchRate $item.BranchRate
}

Write-Host "`nCoverage summary complete." -ForegroundColor Green
