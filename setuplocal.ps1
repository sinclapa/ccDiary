<# --------------------------------------------------------------------------------- #>
<# Utility Functions #>
function ConvertTo-StringData {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline)]
        [HashTable[]]$HashTable
    )
    process {
        foreach ($item in $HashTable) {
            foreach ($entry in $item.GetEnumerator()) {
                "{0}={1}" -f $entry.Key, $entry.Value
            }
        }
    }
}
<# --------------------------------------------------------------------------------- #>


Write-Host "Authenticating with Azure..." -ForegroundColor Cyan

$user = az account show --query "user.name" -o tsv 2>$null
if ($?) { 
    Write-Host "Logged in as: $user" 
} else { 
    az config set core.enable_broker_on_windows=false
    az login
}

$userInfoJson = az ad signed-in-user show --output json | ConvertFrom-Json
$userId = $userInfoJson.id
$userPrincipalName = $userInfoJson.userPrincipalName

# Get tenant information via Azure CLI (replaces Get-AzTenant)
$tenantId = az account show --query "tenantId" --output tsv

# Validate authentication was successful
if (-not $userId -or -not $userPrincipalName -or -not $tenantId) {
    Write-Error "Failed to retrieve Azure authentication information"
    exit 1
}

Write-Host "Authentication successful:" -ForegroundColor Green
Write-Host "  User: $userPrincipalName" -ForegroundColor Gray
Write-Host "  Tenant: $tenantId" -ForegroundColor Gray
Write-Host "  Subscription: $(az account show --query "name" --output tsv)" -ForegroundColor Gray

Write-Host "Configuring Entra App Registration..." -ForegroundColor Cyan
$entraOut = & ".\entraSetup.ps1" `
    -AppName "ccdiary-local-$env:COMPUTERNAME" `
    -spaUris @("https://localhost:54629/swagger/oauth2-redirect.html", "http://localhost:8080/") `
    -webUris @("https://localhost:54629/") `
    -resourceGroupId $env:COMPUTERNAME
$entraClientId = $entraOut.EntraClientId
$entraApplicationIdURI = $entraOut.EntraApplicationIdURI

<# --------------------------------------------------------------------------------- #>
<# Update Local Build Environment #>

Write-Host "Updating Local API Build"
$envPath = ".\src\api\.env"
$localDBPassword = $null
# Read existing DB_PASSWORD from .env if present
if (Test-Path $envPath) {
    $envLines = Get-Content -Path $envPath
    foreach ($line in $envLines) {
        # Skip empty lines and comments
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("#")) {
            continue
        }
        # Split on the first '=' only
        $parts = $line -split '=', 2
        if ($parts.Count -lt 2) {
            continue
        }
        $key = $parts[0].Trim()
        $value = $parts[1]
        if ($key -eq "DB_PASSWORD") {
            $localDBPassword = $value
            break
        }
    }
}
if (-not $localDBPassword) {
    $localDBPassword = Read-Host -Prompt "Enter the password for the local database"
    # Append or create DB_PASSWORD entry in .env
    $dbPasswordLine = "DB_PASSWORD=$localDBPassword"
    if (Test-Path $envPath) {
        Add-Content -Path $envPath -Value $dbPasswordLine
    }
    else {
        $dbPasswordLine | Set-Content -Path $envPath
    }
}

dotnet user-secrets -p .\src\api\ccDiaryApi\ccDiaryApi.csproj init
dotnet user-secrets -p .\src\api\ccDiaryApi\ccDiaryApi.csproj set "SA_PASSWORD" "$localDBPassword"
dotnet user-secrets -p .\src\api\ccDiaryApi\ccDiaryApi.csproj set "Entra:TenantId" "$tenantId"
dotnet user-secrets -p .\src\api\ccDiaryApi\ccDiaryApi.csproj set "Entra:ClientId" "$entraClientId"
dotnet user-secrets -p .\src\api\ccDiaryApi\ccDiaryApi.csproj set "Entra:ApplicationIdUri" "$entraApplicationIdURI"

Write-Host "Updating Local UI Build"
function SetValueInHashTable {
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline)]
        [HashTable]$HashTable,
        [Parameter(Mandatory, Position = 1)]
        [System.String]$Name,
        [Parameter(Mandatory, Position = 2)]
        [System.String]$Value
    )
    if ($HashTable.ContainsKey($Name)) {
        $HashTable[$Name] = $Value
    }
    else {
        $HashTable.Add($Name, $Value)
    }
}

$vuePath = ".\src\ui\.env.dev.local"
if (Test-Path $vuePath) {
    $content = Get-Content -Raw $vuePath | ConvertFrom-StringData
}
else {
    $content = @{}
}
SetValueInHashTable $content "VITE_CLIENT_ID" """$entraClientId"""
SetValueInHashTable $content "VITE_TENANT_ID" """$tenantId"""
SetValueInHashTable $content "VITE_APPLICATION_ID_URI" """$entraApplicationIdURI"""
$content | ConvertTo-StringData | Set-Content $vuePath

Write-Host "Starting local SQL Server instance..." -ForegroundColor Cyan
$containerName = "LocalSqlServer"
$exists = docker ps -a --filter "name=$containerName" --format "{{.Names}}"

if (-not $exists) {
    docker run -p 1433:1433 --name $containerName --rm -d -v local-sql-server-volume:/var/opt/mssql -e ACCEPT_EULA=Y -e SA_PASSWORD=$localDBPassword mcr.microsoft.com/mssql/server:latest
}
<# --------------------------------------------------------------------------------- #>
<# Update Build Pipeline #>
Write-Host "Finished"