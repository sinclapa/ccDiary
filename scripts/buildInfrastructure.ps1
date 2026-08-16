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
<# Setup az extensions #>
if ((az extension list --query "[?name=='containerapp']" ) -eq "[]") {
    az extension add --name containerapp
    Write-Host "Installed containerapp az extension" -ForegroundColor Gray
}

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
if (-Not ($params.ContainsKey("DevApiContainerImage"))) {
    $devApiContainerImage = Read-Host -Prompt "Enter the Dev API Container Image e.g. ghcr.io/OWNER/IMAGE"
    $params.Add("DevApiContainerImage", $devApiContainerImage)
}
else {
    $devApiContainerImage = $params["DevApiContainerImage"]
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


# The bicep template is authoritative for the container spec, so the image it is handed
# replaces whatever is running. DevApiContainerImage is an untagged dev reference, so
# passing it to an environment CI has already promoted rolls that environment back to
# :latest — and unlike the environment variables further down, the script never sets the
# image again afterwards, so the downgrade is permanent and silent. Re-running against a
# live environment must keep the tag that is deployed.
$existingContainerApp = "ca-${name}-${environment}".ToLower()
$existingResourceGroup = "rg-${name}-${environment}"

$deployedImage = az containerapp show `
  --name $existingContainerApp `
  --resource-group $existingResourceGroup `
  --query "properties.template.containers[0].image" `
  --output tsv 2>$null

if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($deployedImage)) {
    $containerImage = $deployedImage.Trim()
    Write-Host "  Preserving the image already deployed: $containerImage" -ForegroundColor Gray
}
else {
    $containerImage = $devApiContainerImage
    Write-Host "  No container app deployed yet; bootstrapping with: $containerImage" -ForegroundColor Gray
}

# The application configuration is applied further down, because it depends on outputs this
# deployment produces. Without feeding the current values back into the template, the
# deployment erases them, the revision fails to start for want of Storage__AccountName, and
# ingress has already sent all traffic to it. Passing them through closes that window.
$existingEnvMap = @{}
$existingSecretRefMap = @{}
$existingSecretMap = @{}

$existingEnvRaw = az containerapp show `
  --name $existingContainerApp `
  --resource-group $existingResourceGroup `
  --query "properties.template.containers[0].env" `
  --output json 2>$null

if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($existingEnvRaw)) {
    foreach ($entry in ($existingEnvRaw | ConvertFrom-Json)) {
        if (-not $entry.name) { continue }

        # A variable carries either an inline value or a reference to a container app
        # secret. The two are preserved separately because the template has to re-declare
        # them in different shapes.
        if ($entry.PSObject.Properties.Name -contains 'secretRef' -and $entry.secretRef) {
            $existingSecretRefMap[$entry.name] = $entry.secretRef
        }
        elseif ($null -ne $entry.value) {
            $existingEnvMap[$entry.name] = $entry.value
        }
    }

    # The secrets themselves must be re-declared too: a template that omits them deletes
    # them, which would leave every secretRef above pointing at nothing.
    $existingSecretsRaw = az containerapp secret list `
      --name $existingContainerApp `
      --resource-group $existingResourceGroup `
      --show-values `
      --output json 2>$null

    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($existingSecretsRaw)) {
        foreach ($secret in ($existingSecretsRaw | ConvertFrom-Json)) {
            if ($secret.name -and $null -ne $secret.value) {
                $existingSecretMap[$secret.name] = $secret.value
            }
        }
    }
}

Write-Host ("  Preserving {0} plain variable(s), {1} secret-backed variable(s) and {2} secret(s)" -f `
    $existingEnvMap.Count, $existingSecretRefMap.Count, $existingSecretMap.Count) -ForegroundColor Gray

# Everything the template needs travels in a parameter file rather than on the command line.
# Two separate failures forced this. PowerShell strips the double quotes out of a JSON literal
# bound for a native command, so az saw `{Key:value}` and rejected it. And `az` on Windows is
# a batch file, so cmd.exe re-parses the command line: a password containing | or & is split
# into fragments and the call fails, or worse, half-succeeds. Neither is reliably escapable —
# keeping the values out of the command line altogether is.
function Invoke-MainDeployment {
    param(
        [System.Collections.IDictionary]$EnvVars,
        [System.Collections.IDictionary]$SecretRefs,
        [System.Collections.IDictionary]$Secrets,
        [string]$Image
    )

    $paramFile = Join-Path ([System.IO.Path]::GetTempPath()) "ccdiary-deploy-$environment-$([guid]::NewGuid().ToString('N')).json"
    $doc = [ordered]@{
        '$schema'      = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
        contentVersion = '1.0.0.0'
        parameters     = [ordered]@{
            existingEnvVars    = [ordered]@{ value = $EnvVars }
            existingSecretRefs = [ordered]@{ value = $SecretRefs }
            existingSecrets    = [ordered]@{ value = $Secrets }
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
          --parameters name=$name environment="$environment" devApiContainerImage=$Image externalDomainName="$externalDomainName" `
          --output json | ConvertFrom-Json
    }
    finally {
        Remove-Item -Path $paramFile -Force -ErrorAction SilentlyContinue
    }
}

$deploymentResult = Invoke-MainDeployment `
    -EnvVars $existingEnvMap `
    -SecretRefs $existingSecretRefMap `
    -Secrets $existingSecretMap `
    -Image $containerImage

# Check if deployment succeeded
if ($LASTEXITCODE -eq 0) {
    Write-Host "Infrastructure deployment completed successfully" -ForegroundColor Green
} else {
    Write-Error "Infrastructure deployment failed"
    exit 1
}

# Extract outputs (PowerShell style)
$resourceGroupName = $deploymentResult.properties.outputs.environment.value.resourceGroupName.value
$containerAppId = $deploymentResult.properties.outputs.environment.value.containerAppId.value
$containerAppName = $deploymentResult.properties.outputs.environment.value.containerAppName.value
$containerAppUrl = $deploymentResult.properties.outputs.environment.value.containerAppUrl.value
$storageAccountName = $deploymentResult.properties.outputs.environment.value.storageAccountName.value
$staticSiteName = $deploymentResult.properties.outputs.environment.value.staticSiteName.value
$staticSiteUrl = $deploymentResult.properties.outputs.environment.value.staticSiteUrl.value
$resourceGroupId = $deploymentResult.properties.outputs.environment.value.resourceGroupId.value
$appName = $deploymentResult.properties.outputs.environment.value.appName.value

Write-Output "  resourceGroupName = $resourceGroupName"
Write-Output "  resourceGroupId = $resourceGroupId"
Write-Output "  containerAppId = $containerAppId"
Write-Output "  containerAppName = $containerAppName"
Write-Output "  containerAppUrl = $containerAppUrl"
Write-Output "  staticSiteName = $staticSiteName"
Write-Output "  staticSiteUrl = $staticSiteUrl"
Write-Output "  appName = $appName"

Write-Host "Configuring Entra App Registration..." -ForegroundColor Cyan

# Build SPA URIs array - add custom domain if configured for prod
$spaUris = @("https://${staticSiteUrl}/", "https://${containerAppUrl}/swagger/oauth2-redirect.html")
if (-not [string]::IsNullOrWhiteSpace($externalDomainName)) {
    $spaUris += "https://${externalDomainName}/"
    Write-Host "  Adding custom domain to SPA URIs: https://${externalDomainName}/" -ForegroundColor Gray
}

$entraOut = & "$PSScriptRoot\entraSetup.ps1" `
    -AppName $appName `
    -spaUris $spaUris `
    -webUris @("https://${containerAppUrl}/") `
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

Write-Host "Storing sensitive configuration as container app secrets..." -ForegroundColor Cyan

# Credentials are held as container app secrets and referenced, rather than set inline.
# Inline values are part of the container spec, so `az containerapp show`, a what-if diff and
# any CLI error that echoes its arguments all print them in full — which is exactly how the
# SMTP password and Graph client secret ended up in a terminal. A secretRef shows the name.
#
# They are written by a second deployment rather than `az containerapp secret set`, because
# the values reach the template through the parameter file and never touch a command line.
# The deployment has to run twice: the Entra client secret cannot exist until the first one
# has produced the URLs the app registration is built from.
#
# An empty secret is rejected, and these settings are genuinely optional — SMTP falls back to
# Entra invitation email and OTLP is disabled when unset — so only non-empty values become
# secrets, and only those get a matching reference. A reference to a secret that does not
# exist stops the revision from starting.
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

Invoke-MainDeployment `
    -EnvVars $existingEnvMap `
    -SecretRefs $secretRefs `
    -Secrets $secretValues `
    -Image $containerImage | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to store container app secrets."
    exit 1
}

Write-Host "  Stored $($secretValues.Count) secret(s)" -ForegroundColor Gray

Write-Host "Updating Container App Environment Variables..." -ForegroundColor Cyan

# Prepare environment variables as an array so PowerShell passes each as a separate argument.
# The three credentials are referenced by secret name rather than carried inline — see the
# secret block above. `secretref:` is the container app syntax for that reference.
$envVars = @(
        "Entra__TenantId=$tenantId",
        "Entra__ClientId=$entraClientId",
        "Entra__ApplicationIdUri=$entraApplicationIdURI",
        "ASPNETCORE_ENVIRONMENT=$environment",
        "Storage__AccountName=$storageAccountName",
        "OTEL_EXPORTER_OTLP_ENDPOINT=$grafanaOtlpEndpoint",
        "OTEL_SERVICE_NAME=ccDiaryApi",
        "Graph__TenantId=$tenantId",
        "Graph__ClientId=$entraClientId",
        "Graph__ClientSecret=secretref:graph-client-secret",
        "Graph__InviteRedirectUrl=https://$staticSiteUrl/",
        "Graph__AppDisplayName=Cooking Code Diary",
        "BootstrapAdmin__ObjectId=$bootstrapAdminObjectId",
        "BootstrapAdmin__Email=$bootstrapAdminEmail",
        "BootstrapAdmin__DisplayName=$bootstrapAdminDisplayName",
        "Smtp__Host=$smtpHost",
        "Smtp__Port=$smtpPort",
        "Smtp__Username=$smtpUsername",
        "Smtp__From=$smtpFrom",
        "Smtp__FromName=$smtpFromName"
)

# Only reference secrets that were actually created; an unset optional setting must not
# become a reference to a secret that does not exist, which the revision would fail on.
if (-not [string]::IsNullOrWhiteSpace($smtpPassword)) {
    $envVars += "Smtp__Password=secretref:smtp-password"
}
if (-not [string]::IsNullOrWhiteSpace($grafanaOtlpAuthHeader)) {
    $envVars += "OTEL_EXPORTER_OTLP_HEADERS=secretref:otlp-headers"
}

az containerapp update `
    --name $containerAppName `
    --resource-group $resourceGroupName `
    --output none `
    --set-env-vars $envVars
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to update Container App environment variables."
    exit 1
}

Write-Host "Create credentials for app container contributor role..." -ForegroundColor Cyan

$azureCredentials = az ad sp create-for-rbac `
  --name "${containerAppName}-credentials" `
  --role contributor `
  --scopes /subscriptions/${subscriptionId}/resourceGroups/${resourceGroupName} `
  --json-auth `
  --output json

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
gh variable set "CONTAINER_APP_NAME" --body "$containerAppName" --repo $gitHubRepo --env "${environment}"
gh variable set "RESOURCE_GROUP_NAME" --body "$resourceGroupName" --repo $gitHubRepo --env "${environment}"
gh variable set "STORAGE_ACCOUNT_NAME" --body "$storageAccountName" --repo $gitHubRepo --env "${environment}"
# The SQL variables are deleted so a stale value cannot be picked up by the deploy workflow.
gh variable delete "SQL_DB_NAME" --repo $gitHubRepo --env "${environment}" 2>$null
gh variable delete "SQL_SERVER_NAME" --repo $gitHubRepo --env "${environment}" 2>$null
gh variable set "API_URL" --body "https://$containerAppUrl/api/" --repo $gitHubRepo --env "${environment}"
gh variable set "ENTRA_CLIENT_ID" --body "$entraClientId" --repo $gitHubRepo --env "${environment}"
gh variable set "ENTRA_APP_OBJECT_ID" --body "$entraObjectId" --repo $gitHubRepo --env "${environment}"
gh variable set "ENTRA_APPLICATION_ID_URI" --body "$entraApplicationIdURI" --repo $gitHubRepo --env "${environment}"
gh variable set "TENANT_ID" --body "$tenantId" --repo $gitHubRepo --env "${environment}"
gh variable set "OTEL_EXPORTER_OTLP_ENDPOINT" --body "$grafanaOtlpEndpoint" --repo $gitHubRepo --env "${environment}"
gh variable set "GRAFANA_FARO_URL" --body "$grafanaFaroUrl" --repo $gitHubRepo --env "${environment}"
# Secrets — credentials and tokens only
gh secret set "AZURE_STATIC_WEB_APPS_API_TOKEN" --body "$token" --repo $gitHubRepo --env "${environment}"
gh secret set "ENTRA_CLIENT_SECRET" --body "${entraClientCredentialsPassword}" --repo $gitHubRepo --env "${environment}"
gh secret set "AZURE_CREDENTIALS" --body "$azureCredentials" --repo $gitHubRepo --env "${environment}"
gh secret set "OTEL_EXPORTER_OTLP_HEADERS" --body "$grafanaOtlpAuthHeader" --repo $gitHubRepo --env "${environment}"
gh variable set "GRAPH_INVITE_REDIRECT_URL" --body "https://$staticSiteUrl/" --repo $gitHubRepo --env "${environment}"
gh variable set "BOOTSTRAP_ADMIN_OBJECT_ID" --body "$bootstrapAdminObjectId" --repo $gitHubRepo --env "${environment}"
gh variable set "BOOTSTRAP_ADMIN_DISPLAY_NAME" --body "$bootstrapAdminDisplayName" --repo $gitHubRepo --env "${environment}"
gh secret set "BOOTSTRAP_ADMIN_EMAIL" --body "$bootstrapAdminEmail" --repo $gitHubRepo --env "${environment}"
if ($smtpHost) {
    gh variable set "SMTP_HOST"      --body "$smtpHost"     --repo $gitHubRepo --env "${environment}"
    gh variable set "SMTP_PORT"      --body "$smtpPort"     --repo $gitHubRepo --env "${environment}"
    gh variable set "SMTP_USERNAME"  --body "$smtpUsername" --repo $gitHubRepo --env "${environment}"
    gh variable set "SMTP_FROM"      --body "$smtpFrom"     --repo $gitHubRepo --env "${environment}"
    gh variable set "SMTP_FROM_NAME" --body "$smtpFromName" --repo $gitHubRepo --env "${environment}"
    gh secret set "SMTP_PASSWORD"    --body "$smtpPassword" --repo $gitHubRepo --env "${environment}"
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



