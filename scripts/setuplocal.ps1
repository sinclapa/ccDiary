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
    $baseUrlApi = "https://$env:CODESPACE_NAME-7183.$env:GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN"
    $baseUrlApiAlt = "https://$env:CODESPACE_NAME-7184.$env:GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN"
    $baseUrlUI = "https://$env:CODESPACE_NAME-8080.$env:GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN"
    Write-Host "  Using Codespace URLs:" -ForegroundColor Gray
    Write-Host "    API: $baseUrlApi" -ForegroundColor Gray
    Write-Host "    API ALT: $baseUrlApiAlt" -ForegroundColor Gray
    Write-Host "    UI: $baseUrlUI" -ForegroundColor Gray
}
else {
    Write-Host "  Using localhost URLs" -ForegroundColor Gray    
    $baseUrlApi = "https://localhost:7183"
    $baseUrlApiAlt = "https://localhost:7184"
    $baseUrlUI = "http://localhost:8080"
}

$entraOut = & "$PSScriptRoot/entraSetup.ps1" `
    -AppName "ccdiary-local-$machineName" `
    -spaUris @("$baseUrlApi/swagger/oauth2-redirect.html", "$baseUrlApiAlt/swagger/oauth2-redirect.html", "$baseUrlUI/") `
    -webUris @("$baseUrlApi/", "$baseUrlApiAlt/") `
    -resourceGroupId $machineName
$entraClientId = $entraOut.EntraClientId
$entraApplicationIdURI = $entraOut.EntraApplicationIdURI
$entraClientSecret = $entraOut.ClientSecret

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


$otlpEndpoint = $apiEnv["OTEL_EXPORTER_OTLP_ENDPOINT"]
if (-not $otlpEndpoint) {
    $otlpEndpoint = Read-Host -Prompt "Enter Grafana Cloud OTLP endpoint (leave empty to disable local telemetry, e.g. https://otlp-gateway-prod-eu-west-0.grafana.net/otlp)"
}

$otlpAuthHeader = $apiEnv["OTEL_EXPORTER_OTLP_HEADERS"]
if (-not $otlpAuthHeader -and $otlpEndpoint) {
    $otlpAuthHeaderSecure = Read-Host -Prompt "Enter Grafana Cloud OTLP auth header (format: Authorization=Basic <base64>)" -AsSecureString
    $otlpAuthHeader = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($otlpAuthHeaderSecure))
}

$smtpHost = $apiEnv["Smtp__Host"]
if (-not $smtpHost) {
    $smtpHost = Read-Host -Prompt "Enter SMTP server hostname (leave empty to use Entra invitation email, e.g. smtp.office365.com)"
}

if ($smtpHost) {
    $smtpPort = $apiEnv["Smtp__Port"]
    if (-not $smtpPort) {
        $smtpPort = Read-Host -Prompt "Enter SMTP port (leave empty for 587)"
        if (-not $smtpPort) { $smtpPort = "587" }
    }

    $smtpUsername = $apiEnv["Smtp__Username"]
    if (-not $smtpUsername) {
        $smtpUsername = Read-Host -Prompt "Enter SMTP username / email address"
    }

    $smtpPassword = $apiEnv["Smtp__Password"]
    if (-not $smtpPassword) {
        $smtpPasswordSecure = Read-Host -Prompt "Enter SMTP password" -AsSecureString
        $smtpPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($smtpPasswordSecure))
    }

    $smtpFrom = $apiEnv["Smtp__From"]
    if (-not $smtpFrom) {
        $smtpFrom = Read-Host -Prompt "Enter From email address (e.g. noreply@yourdomain.com)"
    }

    $smtpFromName = $apiEnv["Smtp__FromName"]
    if (-not $smtpFromName) {
        $smtpFromName = Read-Host -Prompt "Enter From display name (leave empty for 'ccDiary')"
        if (-not $smtpFromName) { $smtpFromName = "ccDiary" }
    }
}
else {
    $smtpPort     = ""
    $smtpUsername = ""
    $smtpPassword = ""
    $smtpFrom     = ""
    $smtpFromName = ""
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
$bootstrapEmail = if ($userInfoJson.mail) { $userInfoJson.mail } else { $userInfoJson.userPrincipalName }
$bootstrapDisplayName = if ($userInfoJson.displayName) { $userInfoJson.displayName } else { $bootstrapEmail }
$apiEnv["BootstrapAdmin__ObjectId"] = $userId
$apiEnv["BootstrapAdmin__Email"] = $bootstrapEmail
$apiEnv["BootstrapAdmin__DisplayName"] = $bootstrapDisplayName
$apiEnv["Graph__TenantId"] = $tenantId
$apiEnv["Graph__ClientId"] = $entraClientId
$apiEnv["Graph__InviteRedirectUrl"] = $baseUrlUI
if ($entraClientSecret) {
    $apiEnv["Graph__ClientSecret"] = $entraClientSecret
}
if ($smtpHost) {
    $apiEnv["Smtp__Host"]     = $smtpHost
    $apiEnv["Smtp__Port"]     = $smtpPort
    $apiEnv["Smtp__Username"] = $smtpUsername
    $apiEnv["Smtp__Password"] = $smtpPassword
    $apiEnv["Smtp__From"]     = $smtpFrom
    $apiEnv["Smtp__FromName"] = $smtpFromName
}
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

$apiProject = "$PSScriptRoot/../src/api/ccDiaryApi/ccDiaryApi.csproj"
dotnet user-secrets -p $apiProject init
dotnet user-secrets -p $apiProject set "Kestrel:Certificates:Development:Password" "$httpsCertPassword"
dotnet user-secrets -p $apiProject set "Entra:TenantId" "$tenantId"
if ($entraClientId) {
    dotnet user-secrets -p $apiProject set "Entra:ClientId" "$entraClientId"
} else {
    Write-Warning 'Entra:ClientId is empty - Entra setup may have failed. Run entraSetup.ps1 manually.'
}
if ($entraApplicationIdURI) {
    dotnet user-secrets -p $apiProject set "Entra:ApplicationIdUri" "$entraApplicationIdURI"
} else {
    Write-Warning 'Entra:ApplicationIdUri is empty - Entra setup may have failed. Run entraSetup.ps1 manually.'
}
if ($otlpEndpoint) {
    dotnet user-secrets -p $apiProject set "OTEL_EXPORTER_OTLP_ENDPOINT" "$otlpEndpoint"
    if ($otlpAuthHeader) {
        dotnet user-secrets -p $apiProject set "OTEL_EXPORTER_OTLP_HEADERS" "$otlpAuthHeader"
    }
}

# Bootstrap admin - the user running setuplocal becomes diary-admin on first API start
$bootstrapEmail = if ($userInfoJson.mail) { $userInfoJson.mail } else { $userInfoJson.userPrincipalName }
$bootstrapDisplayName = if ($userInfoJson.displayName) { $userInfoJson.displayName } else { $bootstrapEmail }
dotnet user-secrets -p $apiProject set "BootstrapAdmin:ObjectId"   "$userId"
dotnet user-secrets -p $apiProject set "BootstrapAdmin:Email"       "$bootstrapEmail"
dotnet user-secrets -p $apiProject set "BootstrapAdmin:DisplayName" "$bootstrapDisplayName"

# Graph API - used by the API to send B2B invitations when approving access requests
$inviteRedirectUrl = $baseUrlUI
dotnet user-secrets -p $apiProject set "Graph:TenantId"        "$tenantId"
if ($entraClientId) {
    dotnet user-secrets -p $apiProject set "Graph:ClientId"        "$entraClientId"
} else {
    Write-Warning 'Graph:ClientId is empty - Entra setup may have failed.'
}
dotnet user-secrets -p $apiProject set "Graph:InviteRedirectUrl" "$inviteRedirectUrl"
if ($entraClientSecret) {
    dotnet user-secrets -p $apiProject set "Graph:ClientSecret" "$entraClientSecret"
}

# SMTP - used by the API to send branded invitation emails when approving access requests
if ($smtpHost) {
    dotnet user-secrets -p $apiProject set "Smtp:Host"     "$smtpHost"
    dotnet user-secrets -p $apiProject set "Smtp:Port"     "$smtpPort"
    dotnet user-secrets -p $apiProject set "Smtp:Username" "$smtpUsername"
    dotnet user-secrets -p $apiProject set "Smtp:Password" "$smtpPassword"
    dotnet user-secrets -p $apiProject set "Smtp:From"     "$smtpFrom"
    dotnet user-secrets -p $apiProject set "Smtp:FromName" "$smtpFromName"
    Write-Host "  SMTP configured ($smtpFrom via $smtpHost)" -ForegroundColor Gray
} else {
    Write-Host "  SMTP not configured — Entra invitation email will be used instead" -ForegroundColor Yellow
}

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

if ($faroUrl) {
    SetValueInHashTable $content "VITE_FARO_URL" """$faroUrl"""
}
$content | ConvertTo-StringData | Set-Content $vuePath

$vueComposePath = "$PSScriptRoot/../src/ui/.env.devcompose.local"
if (Test-Path $vueComposePath) {
    $contentCompose = Get-Content -Raw $vueComposePath | ConvertFrom-StringData
}
else {
    $contentCompose = @{}
}

SetValueInHashTable $contentCompose "VITE_CLIENT_ID" """$entraClientId"""
SetValueInHashTable $contentCompose "VITE_TENANT_ID" """$tenantId"""
SetValueInHashTable $contentCompose "VITE_APPLICATION_ID_URI" """$entraApplicationIdURI"""

if ($faroUrl) {
    SetValueInHashTable $contentCompose "VITE_FARO_URL" """$faroUrl"""
}
$contentCompose | ConvertTo-StringData | Set-Content $vueComposePath

Write-Host "Starting local Azurite instance..." -ForegroundColor Cyan

# Started through compose rather than a bare `docker run`. The compose file already declares
# azurite under the same container name, so a standalone container claimed the name and made
# `docker compose up` fail outright — and the two used different volumes, so data written in
# one mode was invisible in the other. One definition avoids both.
$apiComposeDir = Join-Path $PSScriptRoot "..\src\api"
$apiComposeFile = Join-Path $apiComposeDir "docker-compose.yml"

$azuriteListening = $null -ne (Get-NetTCPConnection -LocalPort 10002 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1)
if ($azuriteListening) {
    Write-Host "  Azurite is already listening on 10002 — leaving it alone." -ForegroundColor Green
}
else {
    # Machines set up before this changed still have the standalone container, which holds
    # the name compose wants. It was created with --rm and its data lives in the old
    # ccdiary-azurite-volume, so removing it costs nothing that startLocal.ps1 does not
    # re-seed; the old volume is left in place rather than deleted.
    $stale = docker ps -a --filter "name=^ccdiary-azurite$" --filter "label=com.docker.compose.project" --format "{{.Names}}"
    $anyExisting = docker ps -a --filter "name=^ccdiary-azurite$" --format "{{.Names}}"
    if ($anyExisting -and -not $stale) {
        Write-Host "  Removing the pre-compose Azurite container so compose can manage it..." -ForegroundColor Yellow
        docker rm -f ccdiary-azurite | Out-Null
    }

    docker compose -p ccdiary -f $apiComposeFile up -d azurite
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to start Azurite. The API cannot start without it." -ForegroundColor Red
    }
}
<# --------------------------------------------------------------------------------- #>
<# Update Build Pipeline #>
Write-Host "Finished" -ForegroundColor Green