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
$settingsFile = "buildInfrastructure.settings"

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
if (-Not ($params.ContainsKey("Environment"))) {
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
if (-Not ($params.ContainsKey("GitHubRepo"))) {
    $gitHubRepo = Read-Host -Prompt "Enter the GitHub Repository e.g. https://github.com/OWNER/REPO"
    $params.Add("GitHubRepo", $gitHubRepo)
}
else {
    $gitHubRepo = $params["GitHubRepo"]
}
if (-Not ($params.ContainsKey("DevApiContainerImage"))) {
    $devApiContainerImage = Read-Host -Prompt "Enter the Dev API Container Image e.g. ghcr.io/OWNER/IMAGE"
    $params.Add("DevApiContainerImage", $devApiContainerImage)
}
else {
    $devApiContainerImage = $params["DevApiContainerImage"]
}
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

# Validate authentication was successful
if (-not $userId -or -not $userPrincipalName -or -not $tenantId) {
    Write-Error "Failed to retrieve Azure authentication information"
    exit 1
}

Write-Host "Authentication successful:" -ForegroundColor Green
Write-Host "  User: $userPrincipalName" -ForegroundColor Gray
Write-Host "  Tenant: $tenantId" -ForegroundColor Gray
Write-Host "  Subscription: $(az account show --query "name" --output tsv)" -ForegroundColor Gray

Write-Host "Starting infrastructure deployment..." -ForegroundColor Cyan

# Deploy using Azure CLI
$deploymentResult = az deployment sub create `
  --location $location `
  --template-file ".\deploy\main.bicep" `
  --parameters name=$name environment="$environment" adminUser=$userPrincipalName adminUserSID=$userId devApiContainerImage=$devApiContainerImage `
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

Write-Output "resourceGroupName = $resourceGroupName"
Write-Output "resourceGroupId = $resourceGroupId"
Write-Output "containerAppId = $containerAppId"
Write-Output "containerAppName = $containerAppName"
Write-Output "containerAppUrl = $containerAppUrl"
Write-Output "databaseServer = $databaseServer"
Write-Output "databaseServerName = $databaseServerName"
Write-Output "databaseId = $databaseId"
Write-Output "databaseName = $databaseName"
Write-Output "staticSiteName = $staticSiteName"
Write-Output "staticSiteUrl = $staticSiteUrl"
Write-Output "appName = $appName"

Write-Host "Configuring Entra App Registration..." -ForegroundColor Cyan
$entraOut = & ".\entraSetup.ps1" `
    -AppName $appName `
    -spaUris @("https://${staticSiteUrl}/", "https://${containerAppUrl}/swagger/oauth2-redirect.html") `
    -webUris @("https://${containerAppUrl}/") `
    -resourceGroupId $resourceGroupId
$entraClientId = $entraOut.EntraClientId
$entraApplicationIdURI = $entraOut.EntraApplicationIdURI

Write-Host "Configuring SQL Firewall Rules..." -ForegroundColor Cyan
$myIP = $(curl -s https://api.ipify.org)

az sql server firewall-rule create `
  --resource-group "${resourceGroupName}" `
  --server "${databaseServerName}" `
  --name "Allow_${env:COMPUTERNAME}_${myIP}" `
  --start-ip-address "${myIP}" `
  --end-ip-address "${myIP}"

Write-Host "Updating Container App Environment Variables..." -ForegroundColor Cyan
az containerapp update `
  --name $containerAppName `
  --resource-group $resourceGroupName `
  --output none `
  --set-env-vars `
    "Entra__TenantId=${tenantId}" `
    "Entra__ClientId=${entraClientId}" `
    "Entra__ApplicationIdUri=${entraApplicationIdURI}" `
    "ASPNETCORE_ENVIRONMENT=UAT" `
    "ConnectionStrings__SqlConnection=Server=tcp:${databaseServer},1433;Initial Catalog=${databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=`"Active Directory Default`";"

Write-Host "Creating Service Connector between Container App and SQL Database..." -ForegroundColor Cyan 
az containerapp connection create sql --connection "sql_$(New-GuidFromString $appName)".Replace("-", "_")  --source-id $containerAppId --target-id $databaseId --client-type dotnet --system-identity -c $containerAppName

Write-Host "Configure GitHub Actions Secrets..." -ForegroundColor Cyan
gh auth status --hostname github.com > $null 2>&1
if ($LASTEXITCODE -eq 0) { 
    Write-Host "gh logged in" 
} else {
     gh auth login --web  
}

$staticSiteSecrets = az staticwebapp secrets list --name "$staticSiteName" --resource-group "$resourceGroupName" --output json | ConvertFrom-Json
$token = $staticSiteSecrets.properties.apiKey
gh secret set "AZURE_STATIC_WEB_APPS_API_TOKEN_${environment}".ToUpper() --body "$token" --repo $gitHubRepo
gh secret set "API_URL_${environment}".ToUpper() --body "$containerAppUrl" --repo $gitHubRepo
gh secret set "ENTRA_CLIENT_ID_${environment}".ToUpper() --body "$entraClientId" --repo $gitHubRepo
gh secret set "ENTRA_APPLICATION_ID_URI_${environment}".ToUpper() --body "$entraApplicationIdURI" --repo $gitHubRepo
gh secret set "TENANT_ID_${environment}".ToUpper() --body "$tenantId" --repo $gitHubRepo

<# --------------------------------------------------------------------------------- #>
<# Update Build Pipeline #>
Write-Host "Finished"



