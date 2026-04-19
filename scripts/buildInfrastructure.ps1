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

if ((az extension list --query "[?name=='serviceconnector-passwordless']" ) -eq "[]") {
    az extension add --name serviceconnector-passwordless --upgrade
    Write-Host "Installed serviceconnector-passwordless az extension" -ForegroundColor Gray
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

# Deploy using Azure CLI
$deploymentResult = az deployment sub create `
  --location $location `
  --template-file "$PSScriptRoot\..\deploy\main.bicep" `
  --parameters name=$name environment="$environment" adminUser=$userPrincipalName adminUserSID=$userId devApiContainerImage=$devApiContainerImage externalDomainName="$externalDomainName" `
  --output json | ConvertFrom-Json

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
$databaseServer = $deploymentResult.properties.outputs.environment.value.databaseServer.value
$databaseServerName = $deploymentResult.properties.outputs.environment.value.databaseServerName.value
$databaseId = $deploymentResult.properties.outputs.environment.value.databaseId.value
$databaseName = $deploymentResult.properties.outputs.environment.value.databaseName.value
$staticSiteName = $deploymentResult.properties.outputs.environment.value.staticSiteName.value
$staticSiteUrl = $deploymentResult.properties.outputs.environment.value.staticSiteUrl.value
$resourceGroupId = $deploymentResult.properties.outputs.environment.value.resourceGroupId.value
$appName = $deploymentResult.properties.outputs.environment.value.appName.value

Write-Output "  resourceGroupName = $resourceGroupName"
Write-Output "  resourceGroupId = $resourceGroupId"
Write-Output "  containerAppId = $containerAppId"
Write-Output "  containerAppName = $containerAppName"
Write-Output "  containerAppUrl = $containerAppUrl"
Write-Output "  databaseServer = $databaseServer"
Write-Output "  databaseServerName = $databaseServerName"
Write-Output "  databaseId = $databaseId"
Write-Output "  databaseName = $databaseName"
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

Write-Host "Configuring SQL Firewall Rules..." -ForegroundColor Cyan
$myIP = Invoke-WebRequest -UseBasicParsing -Uri "https://api.ipify.org"

Write-Host "  Adding firewall rule for IP: $myIP"

az sql server firewall-rule create `
  --resource-group "${resourceGroupName}" `
  --server "${databaseServerName}" `
  --name "Allow_${env:COMPUTERNAME}_${myIP}" `
  --start-ip-address "${myIP}" `
  --end-ip-address "${myIP}" `
  --output none

Write-Host "Set entra client app credentials..." -ForegroundColor Cyan
$entraClientCredentials = az ad app credential reset --id $entraClientId --display-name GIT_HUB --years 2 | ConvertFrom-JSON
$entraClientCredentialsPassword = $entraClientCredentials.password

Write-Host "Updating Container App Environment Variables..." -ForegroundColor Cyan

# Prepare environment variables as an array so PowerShell passes each as a separate argument
$envVars = @(
        "Entra__TenantId=$tenantId",
        "Entra__ClientId=$entraClientId",
        "Entra__ApplicationIdUri=$entraApplicationIdURI",
        "ASPNETCORE_ENVIRONMENT=$environment",
        "RUN_MIGRATIONS=false",
        "OTEL_EXPORTER_OTLP_ENDPOINT=$grafanaOtlpEndpoint",
        "OTEL_EXPORTER_OTLP_HEADERS=$grafanaOtlpAuthHeader",
        "OTEL_SERVICE_NAME=ccDiaryApi",
        "Graph__TenantId=$tenantId",
        "Graph__ClientId=$entraClientId",
        "Graph__ClientSecret=$entraClientCredentialsPassword",
        "Graph__InviteRedirectUrl=https://$staticSiteUrl/",
        "Graph__AppDisplayName=Cooking Code Diary",
        "BootstrapAdmin__ObjectId=$bootstrapAdminObjectId",
        "BootstrapAdmin__Email=$bootstrapAdminEmail",
        "BootstrapAdmin__DisplayName=$bootstrapAdminDisplayName"
)

az containerapp update `
    --name $containerAppName `
    --resource-group $resourceGroupName `
    --output none `
    --set-env-vars $envVars
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to update Container App environment variables."
    exit 1
}

Write-Host "Creating Service Connector between Container App and SQL Database..." -ForegroundColor Cyan
$connectionName = "sql_$((New-GuidFromString $appName).ToString().Replace('-', '_'))"
$existingConnection = az containerapp connection list `
    --source-id $containerAppId `
    --query "[?name=='$connectionName'] | [0].name" -o tsv 2>$null
if ($existingConnection -eq $connectionName) {
    Write-Host "  Service Connector '$connectionName' already exists, skipping." -ForegroundColor Gray
} else {
    az containerapp connection create sql `
        --connection $connectionName `
        --source-id $containerAppId `
        --target-id $databaseId `
        --client-type dotnet `
        --system-identity `
        -c $containerAppName
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to create Service Connector. The Container App cannot connect to SQL without it. Aborting."
        exit 1
    }
    Write-Host "  Service Connector '$connectionName' created successfully." -ForegroundColor Green
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
gh variable set "CONTAINER_APP_NAME".ToUpper() --body "$containerAppName" --repo $gitHubRepo --env "${environment}"
gh variable set "RESOURCE_GROUP_NAME".ToUpper() --body "$resourceGroupName" --repo $gitHubRepo --env "${environment}"
gh secret set "AZURE_STATIC_WEB_APPS_API_TOKEN".ToUpper() --body "$token" --repo $gitHubRepo --env "${environment}"
gh secret set "API_URL".ToUpper() --body "https://$containerAppUrl/api/" --repo $gitHubRepo --env "${environment}"
gh secret set "ENTRA_CLIENT_ID".ToUpper() --body "$entraClientId" --repo $gitHubRepo --env "${environment}"
gh secret set "ENTRA_APP_OBJECT_ID".ToUpper() --body "$entraObjectId" --repo $gitHubRepo --env "${environment}"
gh secret set "ENTRA_CLIENT_SECRET".ToUpper() --body "${entraClientCredentialsPassword}" --repo $gitHubRepo --env "${environment}"
gh secret set "ENTRA_APPLICATION_ID_URI".ToUpper() --body "$entraApplicationIdURI" --repo $gitHubRepo --env "${environment}"
gh secret set "TENANT_ID".ToUpper() --body "$tenantId" --repo $gitHubRepo --env "${environment}"
gh secret set "AZURE_CREDENTIALS".ToUpper() --body "$azureCredentials" --repo $gitHubRepo --env "${environment}"
gh secret set "OTEL_EXPORTER_OTLP_ENDPOINT" --body "$grafanaOtlpEndpoint" --repo $gitHubRepo --env "${environment}"
gh secret set "OTEL_EXPORTER_OTLP_HEADERS" --body "$grafanaOtlpAuthHeader" --repo $gitHubRepo --env "${environment}"
gh secret set "GRAFANA_FARO_URL" --body "$grafanaFaroUrl" --repo $gitHubRepo --env "${environment}"
gh secret set "GRAPH_INVITE_REDIRECT_URL" --body "https://$staticSiteUrl/" --repo $gitHubRepo --env "${environment}"
gh secret set "BOOTSTRAP_ADMIN_OBJECT_ID" --body "$bootstrapAdminObjectId" --repo $gitHubRepo --env "${environment}"
gh secret set "BOOTSTRAP_ADMIN_EMAIL" --body "$bootstrapAdminEmail" --repo $gitHubRepo --env "${environment}"
gh secret set "BOOTSTRAP_ADMIN_DISPLAY_NAME" --body "$bootstrapAdminDisplayName" --repo $gitHubRepo --env "${environment}"

Write-Host "Configure SonarCloud GitHub Variables and Secrets..." -ForegroundColor Cyan
gh variable set "SONAR_API_PROJECT_KEY" --body "$sonarApiProjectKey" --repo $gitHubRepo
gh variable set "SONAR_UI_PROJECT_KEY" --body "$sonarUiProjectKey" --repo $gitHubRepo
gh variable set "SONAR_INFRA_PROJECT_KEY" --body "$sonarInfraProjectKey" --repo $gitHubRepo
gh variable set "SONAR_ORGANIZATION" --body "$sonarOrganization" --repo $gitHubRepo
gh secret set "SONAR_TOKEN" --body "$sonarToken" --repo $gitHubRepo
<# --------------------------------------------------------------------------------- #>
<# Update Build Pipeline #>
Write-Host "Finished ${name} ${environment}" -ForegroundColor Green



