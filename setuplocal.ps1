
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

$machineName = $env:COMPUTERNAME
if ([string]::IsNullOrWhiteSpace($machineName)) {
    $machineName = $env:HOSTNAME
}
if ([string]::IsNullOrWhiteSpace($machineName)) {
    $machineName = [System.Net.Dns]::GetHostName()
}
if ([string]::IsNullOrWhiteSpace($machineName)) {
    Write-Error "Failed to resolve machine name"
    exit 1
}

Write-Host "Configuring Entra App Registration..." -ForegroundColor Cyan

# Detect if running in GitHub Codespace and build appropriate URLs
if ($env:CODESPACE_NAME -and $env:GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN) {
    Write-Host "  Detected GitHub Codespace environment" -ForegroundColor Gray
    $baseUrlApi = "https://$env:CODESPACE_NAME-54628.$env:GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN"
    $baseUrl8080 = "https://$env:CODESPACE_NAME-8080.$env:GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN"
    Write-Host "  Using Codespace URLs:" -ForegroundColor Gray
    Write-Host "    API (54629): $baseUrlApi" -ForegroundColor Gray
    Write-Host "    UI (8080): $baseUrl8080" -ForegroundColor Gray
}
else {
    Write-Host "  Using localhost URLs" -ForegroundColor Gray
    $baseUrlApi = "https://localhost:54629"
    $baseUrl8080 = "http://localhost:8080"
}

$entraOut = & "./entraSetup.ps1" `
    -AppName "ccdiary-local-$machineName" `
    -spaUris @("$baseUrlApi/swagger/oauth2-redirect.html", "$baseUrl8080/") `
    -webUris @("$baseUrlApi/") `
    -resourceGroupId $machineName
$entraClientId = $entraOut.EntraClientId
$entraApplicationIdURI = $entraOut.EntraApplicationIdURI

<# --------------------------------------------------------------------------------- #>
<# Update Local Build Environment #>

Write-Host "Updating Local API Build"
$envPath = "./src/api/.env"
$apiEnv = @{}
if (Test-Path $envPath) {
    $envLines = Get-Content -Path $envPath
    foreach ($line in $envLines) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("#")) {
            continue
        }
        $parts = $line -split '=', 2
        if ($parts.Count -lt 2) {
            continue
        }
        $key = $parts[0].Trim()
        $value = $parts[1]
        if (-not [string]::IsNullOrWhiteSpace($key)) {
            $apiEnv[$key] = $value
        }
    }
}

$localDBPassword = $apiEnv["DB_PASSWORD"]
if (-not $localDBPassword) {
    $localDBPassword = Read-Host -Prompt "Enter the password for the local database"
}

$httpsCertFile = $apiEnv["HTTPS_CERT_FILE"]
if (-not $httpsCertFile) {
    $httpsCertFile = "ccdiaryapi.pfx"
}

$httpsCertPassword = $apiEnv["HTTPS_CERT_PASSWORD"]
if (-not $httpsCertPassword) {
    $httpsCertPassword = "local-dev-cert-password"
}

$userSecretsPath = $null
$httpsCertsPath = $null
$composeFiles = $null
$httpsCertOutputPath = $null
if ($IsWindows -or $env:OS -eq "Windows_NT") {
    $userSecretsPath = Join-Path $env:APPDATA "Microsoft\UserSecrets"
    $httpsCertsPath = Join-Path $PSScriptRoot ".certs\https"
    $composeFiles = "docker-compose.yml;docker-compose.override.yml"
}
else {
    $userSecretsPath = Join-Path $HOME ".microsoft/usersecrets"
    $httpsCertsPath = Join-Path $PSScriptRoot ".certs/https"
    $composeFiles = "docker-compose.yml:docker-compose.override.yml:docker-compose.linux.override.yml"
}

New-Item -ItemType Directory -Path $httpsCertsPath -Force | Out-Null
$httpsCertOutputPath = Join-Path $httpsCertsPath $httpsCertFile
dotnet dev-certs https -ep $httpsCertOutputPath -p $httpsCertPassword | Out-Null

if (-not (Test-Path $httpsCertOutputPath)) {
    Write-Error "Failed to create HTTPS certificate at path: $httpsCertOutputPath"
    exit 1
}

$apiEnv["DB_PASSWORD"] = $localDBPassword
$apiEnv["USER_SECRETS_PATH"] = $userSecretsPath
$apiEnv["HTTPS_CERTS_PATH"] = $httpsCertsPath
$apiEnv["HTTPS_CERT_FILE"] = $httpsCertFile
$apiEnv["HTTPS_CERT_PASSWORD"] = $httpsCertPassword
$apiEnv["COMPOSE_FILE"] = $composeFiles
$apiEnv["Entra__TenantId"] = $tenantId
$apiEnv["Entra__ClientId"] = $entraClientId
$apiEnv["Entra__ApplicationIdUri"] = $entraApplicationIdURI
$apiEnv | ConvertTo-StringData | Set-Content -Path $envPath
Write-Host "  USER_SECRETS_PATH set to: $userSecretsPath" -ForegroundColor Gray
Write-Host "  HTTPS_CERTS_PATH set to: $httpsCertsPath" -ForegroundColor Gray
Write-Host "  HTTPS_CERT_FILE set to: $httpsCertFile" -ForegroundColor Gray
Write-Host "  COMPOSE_FILE set to: $composeFiles" -ForegroundColor Gray

dotnet user-secrets -p ./src/api/ccDiaryApi/ccDiaryApi.csproj init
dotnet user-secrets -p ./src/api/ccDiaryApi/ccDiaryApi.csproj set "SA_PASSWORD" "$localDBPassword"
dotnet user-secrets -p ./src/api/ccDiaryApi/ccDiaryApi.csproj set "Entra:TenantId" "$tenantId"
dotnet user-secrets -p ./src/api/ccDiaryApi/ccDiaryApi.csproj set "Entra:ClientId" "$entraClientId"
dotnet user-secrets -p ./src/api/ccDiaryApi/ccDiaryApi.csproj set "Entra:ApplicationIdUri" "$entraApplicationIdURI"

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

$vuePath = "./src/ui/.env.dev.local"
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