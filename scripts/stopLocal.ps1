[CmdletBinding()]
param(
    [int]$ApiHttpPort  = 5120,
    [int]$ApiHttpsPort = 7183,
    [int]$UiPort       = 8080
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Process names that must never be killed
$protected = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('Code', 'devenv', 'vsls-agent', 'ServiceHub.Host.dotnet.x64'),
    [System.StringComparer]::OrdinalIgnoreCase
)

# Process names where we stop climbing the parent chain
# (these are shells/terminals the user may be actively using)
$shellHosts = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@('pwsh', 'powershell', 'WindowsTerminal', 'conhost', 'cmd', 'bash', 'explorer',
                 'svchost', 'wininit', 'services', 'lsass', 'System'),
    [System.StringComparer]::OrdinalIgnoreCase
)

$wmiProcs = Get-CimInstance Win32_Process

function Get-WmiById([int]$Id) {
    return $wmiProcs | Where-Object { $_.ProcessId -eq $Id } | Select-Object -First 1
}

function Get-ProcessName([int]$Id) {
    $w = Get-WmiById -Id $Id
    if ($null -eq $w) { return $null }
    return [System.IO.Path]::GetFileNameWithoutExtension($w.Name)
}

# Walk up the parent chain and return the highest ancestor we are allowed to kill.
# Stops climbing when it hits a shell host, a protected process, or a missing/cyclic parent.
function Find-KillRoot([int]$ChildPid) {
    $visited  = @{}
    $best     = $ChildPid
    $current  = $ChildPid

    while ($true) {
        if ($visited.ContainsKey($current)) { break }
        $visited[$current] = $true

        $w = Get-WmiById -Id $current
        if ($null -eq $w) { break }

        $parentId = [int]$w.ParentProcessId
        if ($parentId -eq 0 -or $parentId -eq $current) { break }

        $parentName = Get-ProcessName -Id $parentId
        if ($null -eq $parentName) { break }
        if ($protected.Contains($parentName) -or $shellHosts.Contains($parentName)) { break }

        $best    = $parentId
        $current = $parentId
    }

    return $best
}

# Recursively kill a process and all its children (depth-first, children first).
function Stop-Tree([int]$RootPid, [string]$Role) {
    $name = Get-ProcessName -Id $RootPid
    if ($null -eq $name) { return }

    if ($protected.Contains($name)) {
        Write-Host "  Skipping protected process '$name' (PID $RootPid)" -ForegroundColor DarkYellow
        return
    }

    $children = $wmiProcs | Where-Object { $_.ParentProcessId -eq $RootPid }
    foreach ($child in $children) {
        Stop-Tree -RootPid ([int]$child.ProcessId) -Role $Role
    }

    if (Get-Process -Id $RootPid -ErrorAction SilentlyContinue) {
        Write-Host "  Stopping [$Role] $name (PID $RootPid)" -ForegroundColor Cyan
        Stop-Process -Id $RootPid -Force -ErrorAction SilentlyContinue
    }
}

function Stop-PortOwner([int[]]$Ports, [string]$Role) {
    foreach ($port in $Ports) {
        $conn = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
                Select-Object -First 1
        if ($null -eq $conn) { continue }

        $ownerPid = [int]$conn.OwningProcess
        $ownerName = Get-ProcessName -Id $ownerPid
        Write-Host "$Role found on port $port — $ownerName (PID $ownerPid)" -ForegroundColor Yellow

        $killRoot = Find-KillRoot -ChildPid $ownerPid
        Stop-Tree -RootPid $killRoot -Role $Role
        return $true
    }

    return $false
}

$apiStopped = Stop-PortOwner -Ports @($ApiHttpPort, $ApiHttpsPort) -Role 'API'
$uiStopped  = Stop-PortOwner -Ports @($UiPort) -Role 'UI'

if (-not $apiStopped) {
    Write-Host "API not running on ports $ApiHttpPort/$ApiHttpsPort — nothing to stop." -ForegroundColor DarkGray
}

if (-not $uiStopped) {
    Write-Host "UI not running on port $UiPort — nothing to stop." -ForegroundColor DarkGray
}

if ($apiStopped -or $uiStopped) {
    Write-Host "`nDone." -ForegroundColor Green
}
