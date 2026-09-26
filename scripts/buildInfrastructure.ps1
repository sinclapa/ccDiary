<#
.SYNOPSIS
    Builds and deploys infrastructure for a specified environment.

.PARAMETER EnvironmentParam
    Optional. The environment to deploy (e.g., dev, staging, prod). 
    If not provided, the script will use the value from buildInfrastructure.settings or prompt for it.

.EXAMPLE
    .\buildInfrastructure.ps1
    Runs interactively, using settings file or prompting for values.

.EXAMPLE
    .\buildInfrastructure.ps1 -EnvironmentParam staging
    Deploys to the staging environment, overriding any value in the settings file.
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$EnvironmentParam
)

<# --------------------------------------------------------------------------------- #>
<# Utility Functions #>
# Convert a Hashtable to string data format (key=value)
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

# Check a SonarCloud token against the API before the script pushes it to the repo-level
# SONAR_TOKEN secret. A stale value there is silent and expensive: every later CI run fails
# on a quality gate that never reports, and the logs show sonar.token="" rather than the
# masked *** a real secret renders as.
function Test-SonarToken {
    param([string]$Token)

    if ([string]::IsNullOrWhiteSpace($Token)) { return $false }

    try {
        $response = Invoke-RestMethod `
            -Uri "https://sonarcloud.io/api/authentication/validate" `
            -Headers @{ Authorization = "Bearer $Token" } `
            -Method Get `
            -ErrorAction Stop
        return [bool]$response.valid
    } catch {
        return $false
    }
}

# Function to generate a deterministic GUID from a string using SHA-256 (not for security-sensitive uses)
function New-GuidFromString {
    param([string]$InputString)
    $hasher = [System.Security.Cryptography.SHA256]::Create()
    $hashBytes = $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($InputString))
    # SHA-256 produces 32 bytes; use the first 16 bytes to construct a GUID
    $guidBytes = New-Object byte[] 16
    [Array]::Copy($hashBytes, $guidBytes, 16)
    return [System.Guid]::new($guidBytes)
}

<# --------------------------------------------------------------------------------- #>
<# Capture inputs #>
$settingsFile = Join-Path $PSScriptRoot "buildInfrastructure.settings"

if (Test-Path $settingsFile) {
    $params = Get-Content -Raw $settingsFile | ConvertFrom-StringData
}
else {
    $params = @{}
}

if (-Not ($params.ContainsKey("Name"))) {
    $name = Read-Host -Prompt "Enter the name of the project"
    $params.Add("Name", $name)
}
else {
    $name = $params["Name"]
}

# Handle environment parameter - command line argument takes precedence
if ($PSBoundParameters.ContainsKey('EnvironmentParam')) {
    $environment = $EnvironmentParam
    Write-Host "Using environment from command line parameter: $environment" -ForegroundColor Gray
    # Update settings file with the new environment value
    $params["Environment"] = $environment
}
elseif (-Not ($params.ContainsKey("Environment"))) {
    $environment = Read-Host -Prompt "Enter the environment name"
    $params.Add("Environment", $environment)
}
else {
    $environment = $params["Environment"]
}

if (-Not ($params.ContainsKey("Location"))) {
    $location = Read-Host -Prompt "Enter the Azure location e.g. westeurope"
    $params.Add("Location", $location)
}
else {
    $location = $params["Location"]
}
if (-Not ($params.ContainsKey("GitHubOwnerRepo"))) {
    $gitHubOwnerRepo = Read-Host -Prompt "Enter the GitHub Owner/Repo e.g. last part from https://github.com/OWNER/REPO"
    $gitHubRepo = "https://github.com/${gitHubOwnerRepo}"
    $params.Add("GitHubOwnerRepo", $gitHubRepo)
}
else {
    $gitHubOwnerRepo = $params["GitHubOwnerRepo"]
    $gitHubRepo = "https://github.com/${gitHubOwnerRepo}"
}
if (-Not ($params.ContainsKey("ExternalDomainName"))) {
    $externalDomainName = Read-Host -Prompt "Enter the external domain name for prod (leave empty to skip)"
    $params.Add("ExternalDomainName", $externalDomainName)
}
else {
    $externalDomainName = $params["ExternalDomainName"]
}

# Override to empty string if not prod environment
if ($environment -ne "prod") {
    $externalDomainName = ""
}

if (-Not ($params.ContainsKey("SonarApiProjectKey"))) {
    $sonarApiProjectKey = Read-Host -Prompt "Enter the SonarCloud API project key (e.g. cookingcode_ccDiary_api)"
    $params.Add("SonarApiProjectKey", $sonarApiProjectKey)
}
else {
    $sonarApiProjectKey = $params["SonarApiProjectKey"]
}

if (-Not ($params.ContainsKey("SonarUiProjectKey"))) {
    $sonarUiProjectKey = Read-Host -Prompt "Enter the SonarCloud UI project key (e.g. cookingcode_ccDiary_ui)"
    $params.Add("SonarUiProjectKey", $sonarUiProjectKey)
}
else {
    $sonarUiProjectKey = $params["SonarUiProjectKey"]
}

if (-Not ($params.ContainsKey("SonarInfraProjectKey"))) {
    $sonarInfraProjectKey = Read-Host -Prompt "Enter the SonarCloud Infra project key (e.g. cookingcode_ccDiary_infra)"
    $params.Add("SonarInfraProjectKey", $sonarInfraProjectKey)
}
else {
    $sonarInfraProjectKey = $params["SonarInfraProjectKey"]
}

if (-Not ($params.ContainsKey("SonarOrganization"))) {
    $sonarOrganization = Read-Host -Prompt "Enter the SonarQube organization (e.g. name)"
    $params.Add("SonarOrganization", $sonarOrganization)
}
else {
    $sonarOrganization = $params["SonarOrganization"]
}

if (-Not ($params.ContainsKey("SonarToken"))) {
    $sonarToken = Read-Host -Prompt "Enter the SonarQube access token" -AsSecureString
    $sonarToken = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($sonarToken))
    $params.Add("SonarToken", $sonarToken)
}
else {
    $sonarToken = $params["SonarToken"]
}

# Verified here, before any Azure work, so a bad token costs a prompt rather than a deploy.
# The token is written to the settings file below, so the following environments in a
# buildAllInfrastructure run pick up the corrected value without asking again.
$sonarTokenAttempts = 0
while (-not (Test-SonarToken -Token $sonarToken)) {
    $sonarTokenAttempts++
    if ($sonarTokenAttempts -gt 3) {
        Write-Error "No valid SonarCloud token supplied after 3 attempts. Aborting before any infrastructure changes."
        exit 1
    }

    Write-Host "The stored SonarCloud token is missing, expired or rejected by sonarcloud.io." -ForegroundColor Yellow
    $sonarTokenSecure = Read-Host -Prompt "Enter a valid SonarCloud token (My Account > Security > Generate Tokens)" -AsSecureString
    $sonarToken = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($sonarTokenSecure))

    if ([string]::IsNullOrWhiteSpace($sonarToken)) {
        Write-Error "A valid SonarCloud token is required — it is pushed to the repo SONAR_TOKEN secret and gates CI."
        exit 1
    }
}
$params["SonarToken"] = $sonarToken
Write-Host "SonarCloud token validated." -ForegroundColor Green

if (-Not ($params.ContainsKey("GrafanaOtlpEndpoint"))) {
    $grafanaOtlpEndpoint = Read-Host -Prompt "Enter the Grafana Cloud OTLP endpoint (leave empty to disable telemetry, e.g. https://otlp-gateway-prod-eu-west-0.grafana.net/otlp)"
    $params.Add("GrafanaOtlpEndpoint", $grafanaOtlpEndpoint)
}
else {
    $grafanaOtlpEndpoint = $params["GrafanaOtlpEndpoint"]
}

if (-Not ($params.ContainsKey("GrafanaInstanceId"))) {
    $grafanaInstanceId = Read-Host -Prompt "Enter the Grafana Cloud instance ID (numeric, found on the OTLP connection page)"
    $params.Add("GrafanaInstanceId", $grafanaInstanceId)
}
else {
    $grafanaInstanceId = $params["GrafanaInstanceId"]
}

if (-Not ($params.ContainsKey("GrafanaApiToken"))) {
    $grafanaApiTokenSecure = Read-Host -Prompt "Enter the Grafana Cloud API token (scopes: metrics:write, logs:write, traces:write)" -AsSecureString
    $grafanaApiToken = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($grafanaApiTokenSecure))
    $params.Add("GrafanaApiToken", $grafanaApiToken)
}
else {
    $grafanaApiToken = $params["GrafanaApiToken"]
}

$grafanaOtlpAuthHeader = ""
if ($grafanaInstanceId -and $grafanaApiToken) {
    $grafanaOtlpAuthHeaderBytes = [System.Text.Encoding]::UTF8.GetBytes("${grafanaInstanceId}:${grafanaApiToken}")
    $grafanaOtlpAuthHeader = "Authorization=Basic $([System.Convert]::ToBase64String($grafanaOtlpAuthHeaderBytes))"
}

if (-Not ($params.ContainsKey("GrafanaFaroUrl"))) {
    $grafanaFaroUrl = Read-Host -Prompt "Enter the Grafana Cloud Faro collector URL (leave empty to disable frontend telemetry, e.g. https://faro-collector-prod-eu-west-0.grafana.net/collect/<appId>)"
    $params.Add("GrafanaFaroUrl", $grafanaFaroUrl)
}
else {
    $grafanaFaroUrl = $params["GrafanaFaroUrl"]
}

if (-Not ($params.ContainsKey("SmtpHost"))) {
    $smtpHost = Read-Host -Prompt "Enter the SMTP server hostname (e.g. smtp.office365.com, leave empty to use Entra invitation email)"
    $params.Add("SmtpHost", $smtpHost)
}
else {
    $smtpHost = $params["SmtpHost"]
}

if ($smtpHost) {
    if (-Not ($params.ContainsKey("SmtpPort"))) {
        $smtpPort = Read-Host -Prompt "Enter the SMTP port (587 for STARTTLS)"
        if (-not $smtpPort) { $smtpPort = "587" }
        $params.Add("SmtpPort", $smtpPort)
    }
    else {
        $smtpPort = $params["SmtpPort"]
    }

    if (-Not ($params.ContainsKey("SmtpUsername"))) {
        $smtpUsername = Read-Host -Prompt "Enter the SMTP username / email address"
        $params.Add("SmtpUsername", $smtpUsername)
    }
    else {
        $smtpUsername = $params["SmtpUsername"]
    }

    if (-Not ($params.ContainsKey("SmtpPassword"))) {
        $smtpPasswordSecure = Read-Host -Prompt "Enter the SMTP password" -AsSecureString
        $smtpPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($smtpPasswordSecure))
        $params.Add("SmtpPassword", $smtpPassword)
    }
    else {
        $smtpPassword = $params["SmtpPassword"]
    }

    if (-Not ($params.ContainsKey("SmtpFrom"))) {
        $smtpFrom = Read-Host -Prompt "Enter the From email address (e.g. noreply@yourdomain.com)"
        $params.Add("SmtpFrom", $smtpFrom)
    }
    else {
        $smtpFrom = $params["SmtpFrom"]
    }

    if (-Not ($params.ContainsKey("SmtpFromName"))) {
        $smtpFromName = Read-Host -Prompt "Enter the From display name (leave empty for 'ccDiary')"
        if (-not $smtpFromName) { $smtpFromName = "ccDiary" }
        $params.Add("SmtpFromName", $smtpFromName)
    }
    else {
        $smtpFromName = $params["SmtpFromName"]
    }
}
else {
    $smtpPort = ""
    $smtpUsername = ""
    $smtpPassword = ""
    $smtpFrom = ""
    $smtpFromName = ""
}

if (-Not ($params.ContainsKey("BootstrapAdminObjectId"))) {
    $bootstrapAdminObjectId = Read-Host -Prompt "Enter the Bootstrap Admin Entra Object ID (leave empty to skip)"
    $params.Add("BootstrapAdminObjectId", $bootstrapAdminObjectId)
}
else {
    $bootstrapAdminObjectId = $params["BootstrapAdminObjectId"]
}

if (-Not ($params.ContainsKey("BootstrapAdminEmail"))) {
    $bootstrapAdminEmail = Read-Host -Prompt "Enter the Bootstrap Admin email (leave empty to skip)"
    $params.Add("BootstrapAdminEmail", $bootstrapAdminEmail)
}
else {
    $bootstrapAdminEmail = $params["BootstrapAdminEmail"]
}

if (-Not ($params.ContainsKey("BootstrapAdminDisplayName"))) {
    $bootstrapAdminDisplayName = Read-Host -Prompt "Enter the Bootstrap Admin display name (leave empty to skip)"
    $params.Add("BootstrapAdminDisplayName", $bootstrapAdminDisplayName)
}
else {
    $bootstrapAdminDisplayName = $params["BootstrapAdminDisplayName"]
}

# Remove stale key from previous script version
$params.Remove("GrafanaOtlpAuthHeader")

$params | ConvertTo-StringData | Set-Content $settingsFile

<# --------------------------------------------------------------------------------- #>
<# Get Azure Params #>

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
$subscriptionId = az account show --query "id" --output tsv

# Validate authentication was successful
if (-not $userId -or -not $userPrincipalName -or -not $tenantId) {
    Write-Error "Failed to retrieve Azure authentication information"
    exit 1
}

Write-Host "Authentication successful:" -ForegroundColor Green
Write-Host "  User: $userPrincipalName" -ForegroundColor Gray
Write-Host "  Tenant: $tenantId" -ForegroundColor Gray
Write-Host "  Subscription: $(az account show --query "name" --output tsv)" -ForegroundColor Gray
Write-Host "  Subscription ID: $subscriptionId" -ForegroundColor Gray

Write-Host "Starting infrastructure deployment..." -ForegroundColor Cyan
Write-Host "  Configuring environment: ${name}_${environment}" -ForegroundColor Gray


# Everything the template needs travels in a parameter file rather than on the command line.
# Two separate failures forced this. PowerShell strips the double quotes out of a JSON literal
# bound for a native command, so az saw `{Key:value}` and rejected it. And `az` on Windows is
# a batch file, so cmd.exe re-parses the command line: a password containing | or & is split
# into fragments and the call fails, or worse, half-succeeds. Neither is reliably escapable —
# keeping the values out of the command line altogether is.
function Invoke-MainDeployment {
    param(
        # The settings are declared entirely by the template — ARM replaces the whole
        # collection on every deployment — so the full set travels here rather than being
        # applied after the fact. On the first pass they are empty, because most of them depend
        # on the Entra registration this deployment's own outputs produce; the second pass
        # carries the real values.
        [System.Collections.IDictionary]$FunctionAppSettings = @{},
        [System.Collections.IDictionary]$FunctionAppSecretUris = @{}
    )

    $paramFile = Join-Path ([System.IO.Path]::GetTempPath()) "ccdiary-deploy-$environment-$([guid]::NewGuid().ToString('N')).json"
    $doc = [ordered]@{
        '$schema'      = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
        contentVersion = '1.0.0.0'
        parameters     = [ordered]@{
            functionAppSettings   = [ordered]@{ value = $FunctionAppSettings }
            functionAppSecretUris = [ordered]@{ value = $FunctionAppSecretUris }
        }
    }

    # WriteAllText with an explicit BOM-less UTF8: Set-Content emits a BOM under Windows
    # PowerShell, which az refuses to parse.
    [System.IO.File]::WriteAllText(
        $paramFile,
        ($doc | ConvertTo-Json -Depth 6),
        (New-Object System.Text.UTF8Encoding($false)))

    try {
        az deployment sub create `
          --location $location `
          --template-file "$PSScriptRoot\..\deploy\main.bicep" `
          --parameters "@$paramFile" `
          --parameters name=$name environment="$environment" externalDomainName="$externalDomainName" `
          --output json | ConvertFrom-Json
    }
    finally {
        Remove-Item -Path $paramFile -Force -ErrorAction SilentlyContinue
    }
}

$deploymentResult = Invoke-MainDeployment

# Check if deployment succeeded
if ($LASTEXITCODE -eq 0) {
    Write-Host "Infrastructure deployment completed successfully" -ForegroundColor Green
} else {
    Write-Error "Infrastructure deployment failed"
    exit 1
}

# Extract outputs (PowerShell style)
$resourceGroupName = $deploymentResult.properties.outputs.environment.value.resourceGroupName.value
$storageAccountName = $deploymentResult.properties.outputs.environment.value.storageAccountName.value
$staticSiteName = $deploymentResult.properties.outputs.environment.value.staticSiteName.value
$staticSiteUrl = $deploymentResult.properties.outputs.environment.value.staticSiteUrl.value
$resourceGroupId = $deploymentResult.properties.outputs.environment.value.resourceGroupId.value
$appName = $deploymentResult.properties.outputs.environment.value.appName.value
$functionAppName = $deploymentResult.properties.outputs.environment.value.functionAppName.value
$functionAppUrl = $deploymentResult.properties.outputs.environment.value.functionAppUrl.value
$keyVaultName = $deploymentResult.properties.outputs.environment.value.keyVaultName.value
$deploymentContainerName = $deploymentResult.properties.outputs.environment.value.deploymentContainerName.value

Write-Output "  resourceGroupName = $resourceGroupName"
Write-Output "  resourceGroupId = $resourceGroupId"
Write-Output "  staticSiteName = $staticSiteName"
Write-Output "  staticSiteUrl = $staticSiteUrl"
Write-Output "  appName = $appName"
Write-Output "  functionAppName = $functionAppName"
Write-Output "  functionAppUrl = $functionAppUrl"
Write-Output "  keyVaultName = $keyVaultName"

Write-Host "Configuring Entra App Registration..." -ForegroundColor Cyan

# Build SPA URIs array - add custom domain if configured for prod
$spaUris = @(
    "https://${staticSiteUrl}/",
    # Swagger is served by the function app in every environment, and its OAuth flow redirects back
    # to the host it was loaded from.
    "https://${functionAppUrl}/swagger/oauth2-redirect.html"
)
if (-not [string]::IsNullOrWhiteSpace($externalDomainName)) {
    $spaUris += "https://${externalDomainName}/"
    Write-Host "  Adding custom domain to SPA URIs: https://${externalDomainName}/" -ForegroundColor Gray
}

$entraOut = & "$PSScriptRoot\entraSetup.ps1" `
    -AppName $appName `
    -spaUris $spaUris `
    -webUris @("https://${functionAppUrl}/") `
    -resourceGroupId $resourceGroupId
$entraClientId = $entraOut.EntraClientId
$entraApplicationIdURI = $entraOut.EntraApplicationIdURI
$entraObjectId = $entraOut.EntraObjectId

Write-Host "Granting storage data-plane roles to the deploying user..." -ForegroundColor Cyan

# The bicep template grants these to the Container App's managed identity. The person
# running this script needs them too, otherwise the migration tool and any local run
# against the real account get 403s. Note these are data-plane roles: control-plane roles
# such as Storage Account Contributor grant no access to the tables or blobs themselves.
$storageAccountId = az storage account show `
  --name "$storageAccountName" `
  --resource-group "$resourceGroupName" `
  --query "id" -o tsv

foreach ($role in @('Storage Table Data Contributor', 'Storage Blob Data Contributor')) {
    Write-Host "  Granting '$role' to $userPrincipalName"
    az role assignment create `
      --assignee-object-id "$userId" `
      --assignee-principal-type User `
      --role "$role" `
      --scope "$storageAccountId" `
      --output none 2>$null
}

Write-Host "Set entra client app credentials..." -ForegroundColor Cyan

# --append matters: without it, `credential reset` deletes every existing password before
# issuing the new one, so the running app and CI hold a secret that is already invalid for
# the minutes it takes to reach the container app update and the GitHub secret below. If the
# script failed anywhere in between, they stayed broken. Appending leaves the old secret
# working until the new one has been distributed; the superseded ones are pruned at the end,
# once distribution has actually succeeded.
# The credentials to retire are captured before the new one is issued. `credential reset`
# returns only appId/password/tenant — no keyId — so identifying the survivor from its output
# yields null, and a "delete everything except null" filter deletes the new secret too. Taking
# the before-list makes the delete set explicit and incapable of including the new credential.
$priorGitHubKeyIds = @(az ad app credential list `
  --id $entraClientId `
  --query "[?displayName=='GIT_HUB'].keyId" `
  --output tsv | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { $_.Trim() })

$entraClientCredentials = az ad app credential reset --id $entraClientId --display-name GIT_HUB --years 2 --append | ConvertFrom-JSON
$entraClientCredentialsPassword = $entraClientCredentials.password

if (-not $entraClientCredentialsPassword) {
    Write-Error "Failed to create an Entra client secret."
    exit 1
}

# That secret is for the API alone — it reaches the app through Key Vault and is what
# GraphService authenticates with. CI never receives a copy: the workflows that need a Graph
# or API token (the preview redirect steps, and seeding before the end-to-end run) exchange
# the run's own OIDC token against this federated credential instead. Every pull request runs
# in the dev environment, so a stored client secret would be reachable from any branch.
$entraFederatedName = "github-actions-${environment}"
$entraAppObjectId = az ad app show --id $entraClientId --query id -o tsv
$existingEntraFederated = az ad app federated-credential list --id $entraAppObjectId --query "[?name=='$entraFederatedName'].name" -o tsv
if (-not $existingEntraFederated) {
    Write-Host "  Registering federated credential $entraFederatedName on $entraClientId" -ForegroundColor Gray
    $entraFederatedParams = [ordered]@{
        name        = $entraFederatedName
        issuer      = 'https://token.actions.githubusercontent.com'
        subject     = "repo:${gitHubOwnerRepo}:environment:${environment}"
        description = "GitHub Actions gets Graph and API tokens for the $environment environment"
        audiences   = @('api://AzureADTokenExchange')
    } | ConvertTo-Json -Depth 3

    $entraFederatedFile = Join-Path ([System.IO.Path]::GetTempPath()) "ccdiary-entra-federated-$environment.json"
    [System.IO.File]::WriteAllText($entraFederatedFile, $entraFederatedParams, (New-Object System.Text.UTF8Encoding $false))
    az ad app federated-credential create --id $entraAppObjectId --parameters $entraFederatedFile --output none
    Remove-Item $entraFederatedFile -Force
}

Write-Host "Storing sensitive configuration..." -ForegroundColor Cyan

# Only a non-empty value becomes a secret, and only then does the matching setting appear:
# SMTP falls back to Entra invitation email and OTLP is disabled when unset, while a reference
# to a secret that does not exist stops the app from starting. The deployment runs twice
# because the Entra client secret cannot exist until the first pass has produced the URLs the
# app registration is built from.
$secretValues = [ordered]@{ 'graph-client-secret' = $entraClientCredentialsPassword }
$secretRefs = [ordered]@{ 'Graph__ClientSecret' = 'graph-client-secret' }

if (-not [string]::IsNullOrWhiteSpace($smtpPassword)) {
    $secretValues['smtp-password'] = $smtpPassword
    $secretRefs['Smtp__Password'] = 'smtp-password'
}
if (-not [string]::IsNullOrWhiteSpace($grafanaOtlpAuthHeader)) {
    $secretValues['otlp-headers'] = $grafanaOtlpAuthHeader
    $secretRefs['OTEL_EXPORTER_OTLP_HEADERS'] = 'otlp-headers'
}

# The function app holds no credential of its own: its settings carry Key Vault references
# that the platform resolves with the app's managed identity, so `az functionapp config
# appsettings list` shows a URI rather than a password. The vault is RBAC-authorised, so the
# account running this script needs a data-plane role before it can write.
Write-Host "Storing sensitive configuration in Key Vault..." -ForegroundColor Cyan

$keyVaultId = az keyvault show --name $keyVaultName --resource-group $resourceGroupName --query id -o tsv
az role assignment create `
  --assignee-object-id $userId `
  --assignee-principal-type User `
  --role 'Key Vault Secrets Officer' `
  --scope $keyVaultId `
  --output none 2>$null

$functionAppSecretUris = [ordered]@{}
foreach ($secret in $secretValues.GetEnumerator()) {
    # The value travels in a file: az is a batch file, so cmd.exe re-parses the command line
    # after PowerShell has stripped the quotes, and a secret containing | & or () is split
    # into fragments or silently mangled.
    $secretFile = Join-Path ([System.IO.Path]::GetTempPath()) "ccdiary-secret-$([guid]::NewGuid().ToString('N')).txt"
    [System.IO.File]::WriteAllText($secretFile, $secret.Value, (New-Object System.Text.UTF8Encoding($false)))

    try {
        # A freshly granted data-plane role takes a moment to reach the vault, and the first
        # write of a run is the one that meets it — so a 403 here is retried rather than fatal.
        $secretUri = $null
        foreach ($attempt in 1..5) {
            $secretUri = az keyvault secret set `
              --vault-name $keyVaultName `
              --name $secret.Key `
              --file $secretFile `
              --encoding utf-8 `
              --query id -o tsv 2>$null
            if (-not [string]::IsNullOrWhiteSpace($secretUri)) { break }
            Start-Sleep -Seconds 10
        }
    }
    finally {
        Remove-Item -Path $secretFile -Force -ErrorAction SilentlyContinue
    }

    if ([string]::IsNullOrWhiteSpace($secretUri)) {
        Write-Error "Failed to store secret '$($secret.Key)' in $keyVaultName."
        exit 1
    }

    # $secretRefs already maps each setting name to its secret name; invert it so the setting
    # points at the vault instead.
    $settingName = ($secretRefs.GetEnumerator() | Where-Object { $_.Value -eq $secret.Key }).Key
    $functionAppSecretUris[$settingName] = $secretUri.Trim()
}

Write-Host "  Stored $($secretValues.Count) secret(s) in $keyVaultName" -ForegroundColor Gray

# Every non-secret setting the function app runs with. Unlike the container app, whose
# environment is applied after deployment, ARM replaces this collection wholesale — so a
# name missing here is removed from the app, and the deploy workflows set none of them.
$functionAppSettings = [ordered]@{
    'ASPNETCORE_ENVIRONMENT'      = $environment
    # The Functions front end terminates TLS and forwards plain HTTP to the handler on
    # localhost, so the app's own HTTPS redirect would bounce every request.
    'DisableHttpsRedirection'     = 'true'
    'Storage__AccountName'        = $storageAccountName
    'Entra__TenantId'             = $tenantId
    'Entra__ClientId'             = $entraClientId
    'Entra__ApplicationIdUri'     = $entraApplicationIdURI
    'Graph__TenantId'             = $tenantId
    'Graph__ClientId'             = $entraClientId
    'Graph__InviteRedirectUrl'    = "https://${staticSiteUrl}/"
    'Graph__AppDisplayName'       = 'Cooking Code Diary'
    'OTEL_EXPORTER_OTLP_ENDPOINT' = $grafanaOtlpEndpoint
    'OTEL_SERVICE_NAME'           = 'ccDiaryApi'
    'BootstrapAdmin__ObjectId'    = $bootstrapAdminObjectId
    'BootstrapAdmin__Email'       = $bootstrapAdminEmail
    'BootstrapAdmin__DisplayName' = $bootstrapAdminDisplayName
}

if ($smtpHost) {
    $functionAppSettings['Smtp__Host'] = $smtpHost
    $functionAppSettings['Smtp__Port'] = $smtpPort
    $functionAppSettings['Smtp__Username'] = $smtpUsername
    $functionAppSettings['Smtp__From'] = $smtpFrom
    $functionAppSettings['Smtp__FromName'] = $smtpFromName
}

Invoke-MainDeployment `
    -FunctionAppSettings $functionAppSettings `
    -FunctionAppSecretUris $functionAppSecretUris | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to store container app secrets."
    exit 1
}

Write-Host "  Stored $($secretValues.Count) secret(s)" -ForegroundColor Gray

Write-Host "Configure the deploying identity (federated, no stored secret)..." -ForegroundColor Cyan

# The identity keeps its original ca-prefixed name: renaming it would mint a second service
# principal and leave the first one holding live role assignments.
#
# It holds no password. GitHub Actions signs in with a token it mints for the run, which
# Entra trades for an Azure one because of the federated credential registered below — so
# nothing in GitHub stores an Azure credential, and there is none to rotate or leak. The
# subject pins the exchange to this repository *and* this environment, which is why the
# deployment branch policies on the environments matter: they decide which refs can claim it.
$ciCredentialName = "ca-${name}-${environment}-credentials".ToLower()
$ciAppId = az ad app list --filter "displayName eq '$ciCredentialName'" --query "[0].appId" -o tsv
if (-not $ciAppId) {
    Write-Host "  Creating app registration $ciCredentialName" -ForegroundColor Gray
    $ciAppId = az ad app create --display-name "$ciCredentialName" --query appId -o tsv
}

$ciAppObjectId = az ad app show --id $ciAppId --query id -o tsv
$ciPrincipalId = az ad sp show --id $ciAppId --query id -o tsv 2>$null
if (-not $ciPrincipalId) {
    $ciPrincipalId = az ad sp create --id $ciAppId --query id -o tsv
}

$federatedName = "github-actions-${environment}"
$existingFederated = az ad app federated-credential list --id $ciAppObjectId --query "[?name=='$federatedName'].name" -o tsv
if (-not $existingFederated) {
    Write-Host "  Registering federated credential $federatedName" -ForegroundColor Gray
    $federatedParams = [ordered]@{
        name        = $federatedName
        issuer      = 'https://token.actions.githubusercontent.com'
        subject     = "repo:${gitHubOwnerRepo}:environment:${environment}"
        description = "GitHub Actions deploys to the $environment environment"
        audiences   = @('api://AzureADTokenExchange')
    } | ConvertTo-Json -Depth 3

    # BOM-less, like the deployment parameter file: az rejects a file Set-Content has marked.
    $federatedFile = Join-Path ([System.IO.Path]::GetTempPath()) "ccdiary-federated-$environment.json"
    [System.IO.File]::WriteAllText($federatedFile, $federatedParams, (New-Object System.Text.UTF8Encoding $false))
    az ad app federated-credential create --id $ciAppObjectId --parameters $federatedFile --output none
    Remove-Item $federatedFile -Force
}

az role assignment create `
  --assignee-object-id $ciPrincipalId `
  --assignee-principal-type ServicePrincipal `
  --role contributor `
  --scope /subscriptions/${subscriptionId}/resourceGroups/${resourceGroupName} `
  --output none 2>$null

# Contributor is a control-plane role: it can manage the storage account but cannot write a
# blob. The deploy workflows upload the function app's package to the deployment container,
# and shared-key access is disabled on the account, so the credential needs a data-plane
# role as well — without it the upload fails with 403 and the deploy never lands.
$ciClientId = $ciAppId
if ($ciPrincipalId) {
    az role assignment create `
      --assignee-object-id $ciPrincipalId `
      --assignee-principal-type ServicePrincipal `
      --role 'Storage Blob Data Contributor' `
      --scope $storageAccountId `
      --output none 2>$null
}
else {
    Write-Warning "Could not resolve the CI service principal; grant it Storage Blob Data Contributor on $storageAccountName before the next deploy."
}

Write-Host "Configure GitHub Actions Secrets..." -ForegroundColor Cyan
gh auth status --hostname github.com > $null 2>&1
if ($LASTEXITCODE -eq 0) { 
    Write-Host "gh logged in" 
} else {
     gh auth login --web  
}

$staticSiteSecrets = az staticwebapp secrets list --name "$staticSiteName" --resource-group "$resourceGroupName" --output json | ConvertFrom-Json
$token = $staticSiteSecrets.properties.apiKey
gh api --method PUT repos/${gitHubOwnerRepo}/environments/${environment}
# Variables — non-sensitive configuration
# Deleted rather than set: a stale value would point a workflow at a removed resource.
gh variable delete "CONTAINER_APP_NAME" --repo $gitHubRepo --env "${environment}" 2>$null
gh variable set "RESOURCE_GROUP_NAME" --body "$resourceGroupName" --repo $gitHubRepo --env "${environment}"
gh variable set "STORAGE_ACCOUNT_NAME" --body "$storageAccountName" --repo $gitHubRepo --env "${environment}"
# The deploy workflows upload the package to this container and then tell the host to reload.
gh variable set "FUNCTION_APP_NAME" --body "$functionAppName" --repo $gitHubRepo --env "${environment}"
gh variable set "FUNCTION_APP_URL" --body "https://$functionAppUrl" --repo $gitHubRepo --env "${environment}"
gh variable set "DEPLOYMENT_CONTAINER_NAME" --body "$deploymentContainerName" --repo $gitHubRepo --env "${environment}"
# The SQL variables are deleted so a stale value cannot be picked up by the deploy workflow.
gh variable delete "SQL_DB_NAME" --repo $gitHubRepo --env "${environment}" 2>$null
gh variable delete "SQL_SERVER_NAME" --repo $gitHubRepo --env "${environment}" 2>$null
# The UI calls the function app. The container app stays deployed and reachable during the
# migration, so flipping this back is the rollback.
gh variable set "API_URL" --body "https://$functionAppUrl/api/" --repo $gitHubRepo --env "${environment}"
gh variable set "ENTRA_CLIENT_ID" --body "$entraClientId" --repo $gitHubRepo --env "${environment}"
gh variable set "ENTRA_APP_OBJECT_ID" --body "$entraObjectId" --repo $gitHubRepo --env "${environment}"
gh variable set "ENTRA_APPLICATION_ID_URI" --body "$entraApplicationIdURI" --repo $gitHubRepo --env "${environment}"
gh variable set "TENANT_ID" --body "$tenantId" --repo $gitHubRepo --env "${environment}"
# How the workflows sign in to Azure. These are identifiers, not credentials: the proof is
# the run's own OIDC token, exchanged against the federated credential registered above.
gh variable set "AZURE_CLIENT_ID" --body "$ciClientId" --repo $gitHubRepo --env "${environment}"
gh variable set "AZURE_SUBSCRIPTION_ID" --body "$subscriptionId" --repo $gitHubRepo --env "${environment}"
gh variable set "OTEL_EXPORTER_OTLP_ENDPOINT" --body "$grafanaOtlpEndpoint" --repo $gitHubRepo --env "${environment}"
gh variable set "GRAFANA_FARO_URL" --body "$grafanaFaroUrl" --repo $gitHubRepo --env "${environment}"
# Secrets — credentials and tokens only, and only what a workflow actually reads.
#
# Deleted rather than set: every PR runs in the dev environment, so anything stored here is
# reachable from any branch. AZURE_CREDENTIALS and ENTRA_CLIENT_SECRET are both replaced by
# OIDC — the workflows exchange the run's own token. The other three were only ever written,
# never read by a workflow: the application receives those values through Key Vault.
gh secret set "AZURE_STATIC_WEB_APPS_API_TOKEN" --body "$token" --repo $gitHubRepo --env "${environment}"
foreach ($obsoleteSecret in @(
    'AZURE_CREDENTIALS',
    'ENTRA_CLIENT_SECRET',
    'OTEL_EXPORTER_OTLP_HEADERS',
    'BOOTSTRAP_ADMIN_EMAIL',
    'SMTP_PASSWORD')) {
    gh secret delete "$obsoleteSecret" --repo $gitHubRepo --env "${environment}" 2>$null
}
gh variable set "GRAPH_INVITE_REDIRECT_URL" --body "https://$staticSiteUrl/" --repo $gitHubRepo --env "${environment}"
gh variable set "BOOTSTRAP_ADMIN_OBJECT_ID" --body "$bootstrapAdminObjectId" --repo $gitHubRepo --env "${environment}"
gh variable set "BOOTSTRAP_ADMIN_DISPLAY_NAME" --body "$bootstrapAdminDisplayName" --repo $gitHubRepo --env "${environment}"
if ($smtpHost) {
    gh variable set "SMTP_HOST"      --body "$smtpHost"     --repo $gitHubRepo --env "${environment}"
    gh variable set "SMTP_PORT"      --body "$smtpPort"     --repo $gitHubRepo --env "${environment}"
    gh variable set "SMTP_USERNAME"  --body "$smtpUsername" --repo $gitHubRepo --env "${environment}"
    gh variable set "SMTP_FROM"      --body "$smtpFrom"     --repo $gitHubRepo --env "${environment}"
    gh variable set "SMTP_FROM_NAME" --body "$smtpFromName" --repo $gitHubRepo --env "${environment}"
}

Write-Host "Configure SonarCloud GitHub Variables and Secrets..." -ForegroundColor Cyan
gh variable set "SONAR_API_PROJECT_KEY" --body "$sonarApiProjectKey" --repo $gitHubRepo
gh variable set "SONAR_UI_PROJECT_KEY" --body "$sonarUiProjectKey" --repo $gitHubRepo
gh variable set "SONAR_INFRA_PROJECT_KEY" --body "$sonarInfraProjectKey" --repo $gitHubRepo
gh variable set "SONAR_ORGANIZATION" --body "$sonarOrganization" --repo $gitHubRepo
gh secret set "SONAR_TOKEN" --body "$sonarToken" --repo $gitHubRepo

<# --------------------------------------------------------------------------------- #>
<# Retire superseded Entra client secrets #>

# Only now that the new secret is on the container app and in the GitHub environment is it
# safe to withdraw the old ones. Doing this before distribution is what created the outage
# window; doing it never would let credentials accumulate on every run.
Write-Host "Retiring superseded Entra client secrets..." -ForegroundColor Cyan

# Only the credentials that existed before this run are removed, so the one just issued and
# distributed cannot be caught by it however the CLI output is shaped.
if ($priorGitHubKeyIds.Count -gt 0) {
    foreach ($keyId in $priorGitHubKeyIds) {
        Write-Host "  Removing superseded credential $keyId" -ForegroundColor Gray
        az ad app credential delete --id $entraClientId --key-id $keyId --output none 2>$null
    }
}
else {
    Write-Host "  None to retire" -ForegroundColor Gray
}

# A run that ends with no usable secret is the failure this whole section exists to prevent,
# so it is asserted rather than assumed.
#
# The projection deliberately avoids JMESPath's length(): `az` is a batch file on Windows, so
# cmd.exe re-parses the command line after PowerShell has stripped the quotes, and bare
# parentheses are grouping operators to cmd — the call dies with "--output was unexpected at
# this time". Counting in PowerShell keeps the query free of them.
$remainingGitHubCreds = @(az ad app credential list `
  --id $entraClientId `
  --query "[?displayName=='GIT_HUB'].keyId" `
  --output tsv | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

if ($remainingGitHubCreds.Count -lt 1) {
    Write-Error "No GIT_HUB credential remains on the app registration — Graph calls will fail. Investigate before deploying."
    exit 1
}

Write-Host "  $($remainingGitHubCreds.Count) GIT_HUB credential(s) in place" -ForegroundColor Gray

<# --------------------------------------------------------------------------------- #>
<# Update Build Pipeline #>
Write-Host "Finished ${name} ${environment}" -ForegroundColor Green

if ($smtpHost) {
    $smtpDomain = ($smtpFrom -split "@")[-1]
    Write-Host ""
    Write-Host "=== SPF Record ===" -ForegroundColor Yellow

    # The published record is checked rather than assumed. This previously printed
    # "include:$smtpHost" unconditionally, which is wrong twice over: it overwrites a record
    # the mail provider may already have published, and an SMTP submission host is not an SPF
    # include domain. smtp.ionos.co.uk, for one, has no TXT record at all, so including it is
    # a permerror — strictly worse than publishing nothing. Note the script's own examples
    # below are all provider SPF domains, none of them SMTP hostnames.
    $existingSpf = $null
    if (Get-Command -Name Resolve-DnsName -ErrorAction SilentlyContinue) {
        try {
            $existingSpf = Resolve-DnsName -Name $smtpDomain -Type TXT -ErrorAction Stop |
                Where-Object { $_.Strings -and ($_.Strings -join '') -match '^v=spf1' } |
                ForEach-Object { ($_.Strings -join '') } |
                Select-Object -First 1
        } catch {
            $existingSpf = $null
        }
    }

    if ($existingSpf) {
        Write-Host "$smtpDomain already publishes an SPF record:" -ForegroundColor Green
        Write-Host "  $existingSpf" -ForegroundColor White
        Write-Host ""
        Write-Host "No action needed unless mail is sent from somewhere this record does not cover." -ForegroundColor Gray
        Write-Host "Adding a second SPF record is invalid — edit the existing one instead." -ForegroundColor Gray
    }
    else {
        Write-Host "No SPF record found for $smtpDomain. Publish one so invitation email is not" -ForegroundColor Yellow
        Write-Host "treated as spoofed:" -ForegroundColor Yellow
        Write-Host "  Name:  @  (or the root domain itself)" -ForegroundColor White
        Write-Host "  Type:  TXT" -ForegroundColor White
        Write-Host "  Value: v=spf1 include:{provider-spf-domain} ~all" -ForegroundColor White
        Write-Host ""
        Write-Host "Take the include from your mail provider's documentation — it is usually not" -ForegroundColor Gray
        Write-Host "the SMTP hostname you connect to ($smtpHost):" -ForegroundColor Gray
        Write-Host "  Office 365  ->  include:spf.protection.outlook.com" -ForegroundColor Gray
        Write-Host "  Gmail       ->  include:_spf.google.com" -ForegroundColor Gray
        Write-Host "  SendGrid    ->  include:sendgrid.net" -ForegroundColor Gray
        Write-Host "  IONOS       ->  include:_spf-eu.ionos.com" -ForegroundColor Gray
        Write-Host "  Custom      ->  ip4:{your-smtp-server-ip}" -ForegroundColor Gray
    }

    Write-Host ""
    Write-Host "Verify your SPF record at: https://mxtoolbox.com/spf.aspx" -ForegroundColor Gray
}



