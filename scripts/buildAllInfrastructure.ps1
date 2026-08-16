<#
.SYNOPSIS
    Builds and deploys infrastructure for all environments (dev, staging, prod).

.DESCRIPTION
    This script orchestrates the deployment of infrastructure across all environments
    by calling buildInfrastructure.ps1 for each environment sequentially.

    Deployment stops at the first failure. The environments are ordered dev -> staging ->
    prod deliberately: each one is a rehearsal for the next, so a failure in dev is
    evidence the same deployment should not be attempted against prod. Remaining
    environments are reported as Skipped rather than silently omitted.

.PARAMETER Environments
    Optional. Array of environment names to deploy. Default is @("dev", "staging", "prod").

.EXAMPLE
    .\buildAllInfrastructure.ps1
    Deploys infrastructure for dev, staging, and prod environments.

.EXAMPLE
    .\buildAllInfrastructure.ps1 -Environments @("dev", "staging")
    Deploys infrastructure only for dev and staging environments.
#>

param(
    [Parameter(Mandatory=$false)]
    [string[]]$Environments = @("dev", "staging", "prod")
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building Infrastructure for All Environments" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Environments to deploy: $($Environments -join ', ')" -ForegroundColor Gray
Write-Host ""

$startTime = Get-Date
$results = @()
$deploymentFailed = $false

foreach ($env in $Environments) {
    if ($deploymentFailed) {
        $results += [PSCustomObject]@{
            Environment = $env
            Status = "Skipped"
            Duration = "-"
            Error = "Skipped after an earlier environment failed"
        }
        Write-Host "Skipping environment: $env" -ForegroundColor DarkGray
        Write-Host ""
        continue
    }

    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "Starting deployment for environment: $env" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host ""
    
    $envStartTime = Get-Date
    
    try {
        # Call buildInfrastructure.ps1 with the environment parameter
        & "$PSScriptRoot\buildInfrastructure.ps1" -EnvironmentParam $env
        
        if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) {
            throw "buildInfrastructure.ps1 returned exit code $LASTEXITCODE"
        }
        
        $envEndTime = Get-Date
        $duration = $envEndTime - $envStartTime
        
        $results += [PSCustomObject]@{
            Environment = $env
            Status = "Success"
            Duration = $duration.ToString("hh\:mm\:ss")
            Error = $null
        }
        
        Write-Host ""
        Write-Host "✓ Successfully deployed environment: $env in $($duration.ToString("hh\:mm\:ss"))" -ForegroundColor Green
        Write-Host ""
        
    } catch {
        $envEndTime = Get-Date
        $duration = $envEndTime - $envStartTime
        
        $results += [PSCustomObject]@{
            Environment = $env
            Status = "Failed"
            Duration = $duration.ToString("hh\:mm\:ss")
            Error = $_.Exception.Message
        }
        
        $deploymentFailed = $true

        Write-Host ""
        Write-Host "✗ Failed to deploy environment: $env" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "Stopping. Later environments will not be deployed — fix this first," -ForegroundColor Red
        Write-Host "then re-run, optionally narrowing with -Environments." -ForegroundColor Red
        Write-Host ""
    }
}

$endTime = Get-Date
$totalDuration = $endTime - $startTime

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$results | Format-Table -AutoSize

$successCount = ($results | Where-Object { $_.Status -eq "Success" }).Count
$failureCount = ($results | Where-Object { $_.Status -eq "Failed" }).Count
$skippedCount = ($results | Where-Object { $_.Status -eq "Skipped" }).Count

Write-Host ""
Write-Host "Total Duration: $($totalDuration.ToString("hh\:mm\:ss"))" -ForegroundColor Gray
Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $failureCount" -ForegroundColor $(if ($failureCount -gt 0) { "Red" } else { "Gray" })
Write-Host "Skipped: $skippedCount" -ForegroundColor $(if ($skippedCount -gt 0) { "Yellow" } else { "Gray" })
Write-Host ""

if ($failureCount -gt 0) {
    Write-Host "⚠ Deployment stopped at the first failure. Review the errors above." -ForegroundColor Yellow
    if ($skippedCount -gt 0) {
        Write-Host "  $skippedCount environment(s) were left untouched." -ForegroundColor Yellow
    }
    exit 1
} else {
    Write-Host "✓ All environments deployed successfully!" -ForegroundColor Green
    exit 0
}
