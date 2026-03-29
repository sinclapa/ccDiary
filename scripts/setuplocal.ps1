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

$entraOut = & "$PSScriptRoot/entraSetup.ps1" `
    -AppName "ccdiary-local-$machineName" `
    -spaUris @("$baseUrlApi/swagger/oauth2-redirect.html", "$baseUrl8080/") `
    -webUris @("$baseUrlApi/") `
    -resourceGroupId $machineName
$entraClientId = $entraOut.EntraClientId
$entraApplicationIdURI = $entraOut.EntraApplicationIdURI

<# --------------------------------------------------------------------------------- #>
<# Update Local Build Environment #>

Write-Host "Updating Local API Build" -ForegroundColor Cyan
$envPath = "$PSScriptRoot/../src/api/.env"
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

$otlpEndpoint = $apiEnv["OTEL_EXPORTER_OTLP_ENDPOINT"]
if (-not $otlpEndpoint) {
    $otlpEndpoint = Read-Host -Prompt "Enter Grafana Cloud OTLP endpoint (leave empty to disable local telemetry, e.g. https://otlp-gateway-prod-eu-west-0.grafana.net/otlp)"
}

$otlpAuthHeader = $apiEnv["OTEL_EXPORTER_OTLP_HEADERS"]
if (-not $otlpAuthHeader -and $otlpEndpoint) {
    $otlpAuthHeaderSecure = Read-Host -Prompt "Enter Grafana Cloud OTLP auth header (format: Authorization=Basic <base64>)" -AsSecureString
    $otlpAuthHeader = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($otlpAuthHeaderSecure))
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
$onWindows = $IsWindows -or $env:OS -eq "Windows_NT"

if ($onWindows) {
    $userSecretsPath = Join-Path $env:APPDATA "Microsoft\UserSecrets"
    # For Windows, certificates are auto-managed by Visual Studio at this location
    $httpsCertsPath = Join-Path $env:APPDATA "ASP.NET\Https"
    $composeFiles = "docker-compose.yml;docker-compose.override.yml"
    Write-Host "Detected Windows environment" -ForegroundColor Gray
}
else {
    $userSecretsPath = Join-Path $HOME ".microsoft/usersecrets"
    # For Linux/Codespaces, use local .certs directory
    $httpsCertsPath = Join-Path $PSScriptRoot "../.certs/https"
    $composeFiles = "docker-compose.yml:docker-compose.override.yml:docker-compose.linux.override.yml"
    Write-Host "Detected Linux environment" -ForegroundColor Gray
}

Write-Host "Configuring HTTPS certificate..." -ForegroundColor Cyan

if ($onWindows) {
    Write-Host "  Windows detected: Installing development certificate" -ForegroundColor Green
    
    New-Item -ItemType Directory -Path $httpsCertsPath -Force | Out-Null
    $httpsCertOutputPath = Join-Path $httpsCertsPath $httpsCertFile
    
    # Check if certificate exists and validate password
    $needsRegeneration = $false
    if (Test-Path $httpsCertOutputPath) {
        Write-Host "  Certificate found, validating password..." -ForegroundColor Gray
        try {
            # Try to load the certificate with the password
            $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($httpsCertOutputPath, $httpsCertPassword)
            $cert.Dispose()
            Write-Host "  Certificate password is valid, skipping regeneration" -ForegroundColor Green
        } catch {
            Write-Host "  Certificate password is invalid or certificate is corrupted" -ForegroundColor Yellow
            $needsRegeneration = $true
        }
    } else {
        Write-Host "  Certificate not found" -ForegroundColor Gray
        $needsRegeneration = $true
    }
    
    if ($needsRegeneration) {
        # Remove existing certificate if it exists
        if (Test-Path $httpsCertOutputPath) {
            Write-Host "  Removing invalid certificate..." -ForegroundColor Gray
            Remove-Item $httpsCertOutputPath -Force
        }
        
        # Clean existing dev-certs to ensure fresh generation
        Write-Host "  Cleaning existing dev-certs..." -ForegroundColor Gray
        dotnet dev-certs https --clean 2>&1 | Out-Null
        
        # Generate new certificate with the password - this is what Visual Studio does
        Write-Host "  Generating new HTTPS certificate..." -ForegroundColor Gray
        dotnet dev-certs https --trust 2>&1 | Out-Null
        dotnet dev-certs https -ep $httpsCertOutputPath -p $httpsCertPassword 2>&1 | Out-Null
        
        if (-not (Test-Path $httpsCertOutputPath)) {
            Write-Error "Failed to create HTTPS certificate at path: $httpsCertOutputPath"
            exit 1
        }
        
        Write-Host "  Certificate created successfully" -ForegroundColor Green
    }
    
    Write-Host "  Path: $httpsCertOutputPath" -ForegroundColor Gray
    Write-Host "  Password: $httpsCertPassword" -ForegroundColor Gray
} else {
    # Linux/Codespaces: Generate and manage certificates locally
    Write-Host "  Linux detected: Generating development certificate" -ForegroundColor Green
    
    New-Item -ItemType Directory -Path $httpsCertsPath -Force | Out-Null
    $httpsCertOutputPath = Join-Path $httpsCertsPath $httpsCertFile
    
    # Check if certificate exists and validate password
    $needsRegeneration = $false
    if (Test-Path $httpsCertOutputPath) {
        Write-Host "  Certificate found, validating password..." -ForegroundColor Gray
        try {
            # Try to load the certificate with the password
            $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($httpsCertOutputPath, $httpsCertPassword)
            $cert.Dispose()
            Write-Host "  Certificate password is valid, skipping regeneration" -ForegroundColor Green
        } catch {
            Write-Host "  Certificate password is invalid or certificate is corrupted" -ForegroundColor Yellow
            $needsRegeneration = $true
        }
    } else {
        Write-Host "  Certificate not found" -ForegroundColor Gray
        $needsRegeneration = $true
    }
    
    if ($needsRegeneration) {
        # Remove existing certificate if it exists
        if (Test-Path $httpsCertOutputPath) {
            Write-Host "  Removing invalid certificate..." -ForegroundColor Gray
            Remove-Item $httpsCertOutputPath -Force
        }
        
        # Clean existing dev-certs to ensure fresh generation
        Write-Host "  Cleaning existing dev-certs..." -ForegroundColor Gray
        dotnet dev-certs https --clean 2>&1 | Out-Null
        
        # Generate new certificate with the password from config
        Write-Host "  Generating new HTTPS certificate..." -ForegroundColor Gray
        dotnet dev-certs https --trust 2>&1 | Out-Null
        dotnet dev-certs https -ep $httpsCertOutputPath -p $httpsCertPassword 2>&1 | Out-Null
        
        if (-not (Test-Path $httpsCertOutputPath)) {
            Write-Error "Failed to create HTTPS certificate at path: $httpsCertOutputPath"
            exit 1
        }
        
        Write-Host "  Certificate created successfully" -ForegroundColor Green
    }
    
    Write-Host "  Path: $httpsCertOutputPath" -ForegroundColor Gray
    Write-Host "  Password: $httpsCertPassword" -ForegroundColor Gray
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
$apiEnv["OTEL_EXPORTER_OTLP_ENDPOINT"] = $otlpEndpoint
$apiEnv["OTEL_EXPORTER_OTLP_HEADERS"] = $otlpAuthHeader
$apiEnv | ConvertTo-StringData | Set-Content -Path $envPath
Write-Host "  USER_SECRETS_PATH set to: $userSecretsPath" -ForegroundColor Gray
Write-Host "  HTTPS_CERTS_PATH set to: $httpsCertsPath" -ForegroundColor Gray
Write-Host "  HTTPS_CERT_FILE set to: $httpsCertFile" -ForegroundColor Gray
Write-Host "  HTTPS_CERT_PASSWORD set to: $httpsCertPassword" -ForegroundColor Gray
Write-Host "  COMPOSE_FILE set to: $composeFiles" -ForegroundColor Gray
if ($otlpEndpoint) {
    Write-Host "  OTEL_EXPORTER_OTLP_ENDPOINT set (Grafana telemetry enabled)" -ForegroundColor Gray
} else {
    Write-Host "  OTEL_EXPORTER_OTLP_ENDPOINT not set (Grafana telemetry disabled locally)" -ForegroundColor Yellow
}

dotnet user-secrets -p "$PSScriptRoot/../src/api/ccDiaryApi/ccDiaryApi.csproj" init
dotnet user-secrets -p "$PSScriptRoot/../src/api/ccDiaryApi/ccDiaryApi.csproj" set "SA_PASSWORD" "$localDBPassword"
dotnet user-secrets -p "$PSScriptRoot/../src/api/ccDiaryApi/ccDiaryApi.csproj" set "Entra:TenantId" "$tenantId"
dotnet user-secrets -p "$PSScriptRoot/../src/api/ccDiaryApi/ccDiaryApi.csproj" set "Entra:ClientId" "$entraClientId"
dotnet user-secrets -p "$PSScriptRoot/../src/api/ccDiaryApi/ccDiaryApi.csproj" set "Entra:ApplicationIdUri" "$entraApplicationIdURI"

$vuePath = "$PSScriptRoot/../src/ui/.env.dev.local"
if (Test-Path $vuePath) {
    $content = Get-Content -Raw $vuePath | ConvertFrom-StringData
}
else {
    $content = @{}
}

$faroUrl = $content["VITE_FARO_URL"] -replace '^"(.*)"$', '$1'
if (-not $faroUrl -and $otlpEndpoint) {
    $faroUrl = Read-Host -Prompt "Enter Grafana Cloud Faro collector URL (leave empty to disable frontend telemetry, e.g. https://faro-collector-prod-eu-west-0.grafana.net/collect/<appId>)"
}

Write-Host "Updating Local UI Build" -ForegroundColor Cyan
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

SetValueInHashTable $content "VITE_CLIENT_ID" """$entraClientId"""
SetValueInHashTable $content "VITE_TENANT_ID" """$tenantId"""
SetValueInHashTable $content "VITE_APPLICATION_ID_URI" """$entraApplicationIdURI"""
SetValueInHashTable $content "VITE_ENVIRONMENT" """local"""
SetValueInHashTable $content "VITE_APP_VERSION" """0.0.0-local"""
if ($faroUrl) {
    SetValueInHashTable $content "VITE_FARO_URL" """$faroUrl"""
}
$content | ConvertTo-StringData | Set-Content $vuePath

Write-Host "Starting local SQL Server instance..." -ForegroundColor Cyan
$containerName = "LocalSqlServer"
$exists = docker ps -a --filter "name=$containerName" --format "{{.Names}}"

if (-not $exists) {
    docker run -p 1433:1433 --name $containerName --rm -d -v local-sql-server-volume:/var/opt/mssql -e ACCEPT_EULA=Y -e SA_PASSWORD=$localDBPassword mcr.microsoft.com/mssql/server:latest
}
<# --------------------------------------------------------------------------------- #>
<# Update Build Pipeline #>
Write-Host "Finished" -ForegroundColor Green