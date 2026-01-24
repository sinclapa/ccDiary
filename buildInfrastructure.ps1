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

function New-GuidFromString {
    param([string]$InputString)
    $hasher = [System.Security.Cryptography.MD5]::Create()
    $hashBytes = $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($InputString))
    return [System.Guid]::new($hashBytes)
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

az account clear
az config set core.enable_broker_on_windows=false
az login

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
  --parameters name=$name adminUser=$userPrincipalName adminUserSID=$userId devApiContainerImage=$devApiContainerImage `
  --output json | ConvertFrom-Json
  
Write-Host $deploymentResult.outputs.devEnvironment.value.environment
Write-Host $deploymentResult

# Check if deployment succeeded
if ($LASTEXITCODE -eq 0) {
    Write-Host "Infrastructure deployment completed successfully" -ForegroundColor Green
} else {
    Write-Error "Infrastructure deployment failed"
    exit 1
}

# Extract outputs (PowerShell style)
$resourceGroupName = $deploymentResult.properties.outputs.devEnvironment.value.resourceGroupName.value
$containerAppId = $deploymentResult.properties.outputs.devEnvironment.value.containerAppId.value
$containerAppName = $deploymentResult.properties.outputs.devEnvironment.value.containerAppName.value
$containerAppUrl = $deploymentResult.properties.outputs.devEnvironment.value.containerAppUrl.value
$databaseServer = $deploymentResult.properties.outputs.devEnvironment.value.databaseServer.value
$databaseId = $deploymentResult.properties.outputs.devEnvironment.value.databaseId.value
$databaseName = $deploymentResult.properties.outputs.devEnvironment.value.databaseName.value
$staticSiteName = $deploymentResult.properties.outputs.devEnvironment.value.staticSiteName.value
$staticSiteUrl = $deploymentResult.properties.outputs.devEnvironment.value.staticSiteUrl.value
$resourceGroupId = $deploymentResult.properties.outputs.devEnvironment.value.resourceGroupId.value
$appName = $deploymentResult.properties.outputs.devEnvironment.value.appName.value

Write-Output "resourceGroupName = $resourceGroupName"
Write-Output "resourceGroupId = $resourceGroupId"
Write-Output "containerAppName = $containerAppName"
Write-Output "containerAppUrl = $containerAppUrl"
Write-Output "databaseServer = $databaseServer"
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

az containerapp update `
  --name $containerAppName `
  --resource-group $resourceGroupName `
  --output none `
  --set-env-vars `
    "Entra__TenantId=$tenantId" `
    "Entra__ClientId=${entraClientId}" `
    "Entra__ApplicationIdUri=${entraApplicationIdURI}" `
    "ASPNETCORE_ENVIRONMENT=UAT" `
    "ConnectionStrings__SqlConnection=Server=tcp:${databaseServer},1433;Initial Catalog=${databaseName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;"

az containerapp connection create sql --connection "sql_${New-GuidFromString(appName)}" --source-id $containerAppId --target-id $databaseId --client-type dotnet --system-identity -c $containerAppName

exit 1



