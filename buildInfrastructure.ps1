<# --------------------------------------------------------------------------------- #>
<# Setup PowerShell #>

if (-Not (Get-Module -ListAvailable -Name SqlServer)) {
    Install-Module -Name SqlServer -Force
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

if (-Not ($params.ContainsKey("SubscriptionId"))) {
    $subscriptionId = Read-Host -Prompt "Enter the Subscription Id"
    $params.Add("SubscriptionId", $subscriptionId)
}
else {
    $subscriptionId = $params["SubscriptionId"]
}
if (-Not ($params.ContainsKey("Name"))) {
    $name = Read-Host -Prompt "Enter the name of the project"
    $params.Add("Name", $name)
}
else {
    $name = $params["Name"]
}
if (-Not ($params.ContainsKey("Environment"))) {
    $environment = Read-Host -Prompt "Enter the environment e.g. dev"
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
if (-Not ($params.ContainsKey("DevOpsOrg"))) {
    $devOpsOrg = Read-Host -Prompt "Enter the Azure DevOps Org e.g. https://dev.azure.com/orgname/"
    $params.Add("DevOpsOrg", $devOpsOrg)
}
else {
    $devOpsOrg = $params["DevOpsOrg"]
}
if (-Not ($params.ContainsKey("DevOpsProject"))) {
    $devOpsProject = Read-Host -Prompt "Enter the Azure DevOps Project"
    $params.Add("DevOpsProject", $devOpsProject)
}
else {
    $devOpsProject = $params["DevOpsProject"]
}
if (-Not ($params.ContainsKey("DevOpsPipelineName"))) {
    $devOpsPipelineName = Read-Host -Prompt "Enter the Azure DevOps Pipeline Name"
    $params.Add("DevOpsPipelineName", $devOpsPipelineName)
}
else {
    $devOpsPipelineName = $params["DevOpsPipelineName"]
}
if (-Not ($params.ContainsKey("DevOpsPipelineName"))) {
    $devOpsPipelineName = Read-Host -Prompt "Enter the Azure DevOps Pipeline Name"
    $params.Add("DevOpsPipelineName", $devOpsPipelineName)
}
else {
    $devOpsPipelineName = $params["DevOpsPipelineName"]
}

$params | ConvertTo-StringData | Set-Content $settingsFile

<# --------------------------------------------------------------------------------- #>
<# Get Azure Params #>
Connect-AzAccount -Subscription $subscriptionId

$outputUser = Get-AzADUser
$userId = $outputUser.Id
$userPrincipalName = $outputUser.UserPrincipalName

$outputTenant = Get-AzTenant
$tenantId = $outputTenant.Id

az login --tenant $tenantId

$outputInfrastructure = New-AzSubscriptionDeployment -Location $location -TemplateFile .\deploy\main.bicep -nameFromTemplate $name -environment $environment -adminUser $userPrincipalName -adminUserSID $userId
$outputInfrastructure
foreach ($key in $outputInfrastructure.Outputs.keys) {
    if ($key -eq "resourceGroupName") {
        $resourceGroupName = $outputInfrastructure.Outputs[$key].value
    }
    if ($key -eq "containerAppName") {
        $containerAppName = $outputInfrastructure.Outputs[$key].value
    }
    if ($key -eq "containerAppUrl") {
        $containerAppUrl = $outputInfrastructure.Outputs[$key].value
    }
    if ($key -eq "containerRegistryName") {
        $containerRegistryName = $outputInfrastructure.Outputs[$key].value
    }
    if ($key -eq "containerRegistryLoginServer") {
        $containerRegistryLoginServer = $outputInfrastructure.Outputs[$key].value
    }
    if ($key -eq "databaseServer") {
        $databaseServer = $outputInfrastructure.Outputs[$key].value
    }
    if ($key -eq "databaseName") {
        $databaseName = $outputInfrastructure.Outputs[$key].value
    }
    if ($key -eq "staticSiteName") {
        $staticSiteName = $outputInfrastructure.Outputs[$key].value
    }
    if ($key -eq "staticSiteUrl") {
        $staticSiteUrl = $outputInfrastructure.Outputs[$key].value
    }          
    if ($key -eq "resourceGroupId") {
        $resourceGroupId = $outputInfrastructure.Outputs[$key].value
    }
}

Write-Output "resourceGroupName = $resourceGroupName"
Write-Output "resourceGroupId = $resourceGroupId"
Write-Output "containerAppName = $containerAppName"
Write-Output "containerAppUrl = $containerAppUrl"
Write-Output "containerRegistryName = $containerRegistryName"
Write-Output "containerRegistryLoginServer = $containerRegistryLoginServer"
Write-Output "databaseServer = $databaseServer"
Write-Output "databaseName = $databaseName"
Write-Output "staticSiteName = $staticSiteName"
Write-Output "staticSiteUrl = $staticSiteUrl"

<# --------------------------------------------------------------------------------- #>
<# Configure Entra App Registration #>
$app_name="${name}-${environment}"

$appSpaJson = @{redirectUris = @("https://localhost:54629/swagger/oauth2-redirect.html", "http://localhost:8080/", "https://${staticSiteUrl}/", "https://${containerAppUrl}/swagger/oauth2-redirect.html", "https://ccdiary.cookingcode.com/")} | ConvertTo-Json -d 3 -Compress
$appUpdateBody = $appSpaJson | ConvertTo-Json -d 4

$webUris=@("https://localhost:54629/", "https://${containerAppUrl}/")
$appId=$(az ad app list --filter "displayName eq '$app_name'" --query "[0].appId" -o tsv)

if ($appId -ne "" -and $appId -ne $null) {
    Write-Host "Updating existing application: $appId"
    
    # Update application
    az ad app update --id $appId `
        --web-redirect-uris $webUris `
        --set spa=$appUpdateBody `
        --identifier-uris "api://${appId}" `
        --enable-id-token-issuance true `
        --sign-in-audience AzureADMyOrg
} else {
    Write-Host "Creating new application..."
    
    # Create application
    $appId = az ad app create `
        --display-name $app_name `
        --web-redirect-uris $webUris `
        --enable-id-token-issuance true `
        --sign-in-audience AzureADMyOrg `
        --query "appId" -o tsv

    az ad app update --id $appId `
        --set spa=$appUpdateBody `
        --identifier-uris "api://${appId}" `
        --enable-id-token-issuance true `
        --sign-in-audience AzureADMyOrg
}

$existingApp = az ad app show --id $appId | ConvertFrom-Json
if ($existingApp.api.oauth2PermissionScopes) {
    foreach ($scope in $existingApp.api.oauth2PermissionScopes) {
        $scope.isEnabled = $false
    }
    $disabledScopesJson = @{ oauth2PermissionScopes = $existingApp.api.oauth2PermissionScopes } | ConvertTo-Json -Depth 10 -Compress
    $disabledScopesBody = $disabledScopesJson | ConvertTo-Json -d 4
    az ad app update --id $appId --set api=$disabledScopesBody
}

$oauthJson = @(
    @{
        oauth2PermissionScopes = @(
            @{
                id = New-GuidFromString "${resourceGroupId}-${name}-${environment}-oauth2-diary-update"
                value = "Diary.Update"
                adminConsentDisplayName = "Update diary details"
                adminConsentDescription = "Update diary details within the ccDiary API"
                userConsentDescription = $null
                userConsentDisplayName = $null  
                isEnabled = $true
                type = "Admin"
            }
        )
    }
)
$oauthJsonOutput = $oauthJson | ConvertTo-Json -Depth 10 -Compress
$oauthJsonOutputBody = $oauthJsonOutput | ConvertTo-Json -d 4
az ad app update --id $appId --set api=$oauthJsonOutputBody 

$resourceJson = @(
  @{
    resourceAppId = "00000003-0000-0000-c000-000000000000"
    resourceAccess = @(
      @{
        id = New-GuidFromString "${resourceGroupId}-${name}-${environment}-resourceAccess-scope-00000003-0000-0000-c000-000000000000"
        type = "Scope"
      }
    )    
  }
)
$resourceJsonOutput = $resourceJson | ConvertTo-Json -Depth 10 -Compress
$resourceJsonOutputBody = $resourceJsonOutput | ConvertTo-Json -d 4

az ad app update --id $appId --set requiredResourceAccess="[$resourceJsonOutputBody]"

$entraApplicationIdURI = "api://${appId}"
$entraClientId = $appId
Write-Output "entraApplicationIdURI = $entraApplicationIdURI"
Write-Output "entraClientId = $entraClientId"

<# --------------------------------------------------------------------------------- #>
<# Update Local Build Environment #>

Write-Host "Updating Local API Build"
$envPath = ".\src\api\.env"
if (Test-Path $envPath) {
    $envContent = Get-Content -Raw $envPath | ConvertFrom-StringData
}
else {
    $envContent = @{}
}
if (-Not ($envContent.ContainsKey("DB_PASSWORD"))) {
    $localDBPassword = Read-Host -Prompt "Enter the password for the local database"
    $envContent.Add("DB_PASSWORD", $localDBPassword)
    $envContent | ConvertTo-StringData | Set-Content $envPath
}
else {
    $localDBPassword = $envContent["DB_PASSWORD"]
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
SetValueInHashTable $content "VITE_CLIENTID" """$entraClientId"""
SetValueInHashTable $content "VITE_TENANTID" """$tenantId"""
SetValueInHashTable $content "VITE_APPLICATIONID_URI" """$entraApplicationIdURI"""
$content | ConvertTo-StringData | Set-Content $vuePath

<# --------------------------------------------------------------------------------- #>
<# Configure azure database roles #>

Write-Host "Configure database"

$dbToken = (Get-AzAccessToken -AsSecureString -ResourceUrl https://database.windows.net).Token
$dbTokenCredential = [PSCredential]::new("token", $dbToken)

Invoke-Sqlcmd -Query "
IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE type_desc = 'EXTERNAL_USER' and name = '${containerAppName}')
BEGIN
  SELECT 'Add external user ${containerAppName}'
  CREATE USER ""${containerAppName}"" FROM EXTERNAL PROVIDER
END

IF NOT EXISTS(SELECT 1 
          FROM sys.database_role_members AS DRM 
            RIGHT OUTER JOIN sys.database_principals AS DPRole  
              ON DRM.role_principal_id = DPRole.principal_id  
            LEFT OUTER JOIN sys.database_principals AS DPUser  
              ON DRM.member_principal_id = DPUser.principal_id  
          WHERE DPRole.name = 'db_datareader' AND DPUser.name = '${containerAppName}')
BEGIN 
  SELECT 'Add ${containerAppName} to db_datareader'
  ALTER ROLE db_datareader ADD MEMBER ""${containerAppName}"";
END

IF NOT EXISTS(SELECT 1 
          FROM sys.database_role_members AS DRM 
            RIGHT OUTER JOIN sys.database_principals AS DPRole  
              ON DRM.role_principal_id = DPRole.principal_id  
            LEFT OUTER JOIN sys.database_principals AS DPUser  
              ON DRM.member_principal_id = DPUser.principal_id  
          WHERE DPRole.name = 'db_datawriter' AND DPUser.name = '${containerAppName}')
BEGIN 
  SELECT 'Add ${containerAppName} to db_datawriter'
  ALTER ROLE db_datawriter ADD MEMBER ""${containerAppName}"";
END

IF NOT EXISTS(SELECT 1 
          FROM sys.database_role_members AS DRM 
            RIGHT OUTER JOIN sys.database_principals AS DPRole  
              ON DRM.role_principal_id = DPRole.principal_id  
            LEFT OUTER JOIN sys.database_principals AS DPUser  
              ON DRM.member_principal_id = DPUser.principal_id  
          WHERE DPRole.name = 'db_ddladmin' AND DPUser.name = '${containerAppName}')
BEGIN 
  SELECT 'Add ${containerAppName} to db_ddladmin'
  ALTER ROLE db_ddladmin ADD MEMBER ""${containerAppName}"";
END
" -ServerInstance $databaseServer -database $databaseName -AccessToken $dbTokenCredential.GetNetworkCredential().Password

<# --------------------------------------------------------------------------------- #>
<# Update Build Pipeline #>
Write-Host "Update Azure DevOps Pipeline"

function SetDevOpsPipelineVariable {
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline)]
        [PSCustomObject]$Variables,
        [Parameter(Mandatory, Position = 1)]
        [System.String]$Org,
        [Parameter(Mandatory, Position = 2)]
        [System.String]$Project,
        [Parameter(Mandatory, Position = 3)]
        [System.String]$PipelineName,
        [Parameter(Mandatory, Position = 4)]
        [System.String]$Name,
        [Parameter(Mandatory, Position = 5)]
        [System.String]$Value,
        [Parameter(Position = 6)]
        [System.Boolean]$Secret
    )
    if ($Variables.PSObject.Properties.Name.Contains($Name)) {
        az pipelines variable update --org $Org --project $Project --pipeline-name $PipelineName --name $Name --value $Value --secret $Secret
    }
    else {
        az pipelines variable create --org $Org --project $Project --pipeline-name $PipelineName --name $Name --value $Value --secret $Secret
    }
}

$pipelineVariables = az pipelines variable list --org $devOpsOrg --project $devOpsProject --pipeline-name $devOpsPipelineName | ConvertFrom-Json 

$siteDeploymentToken = az staticwebapp secrets list --resource-group $resourceGroupName --name $staticSiteName --query properties.apiKey
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "containerAppName" $containerAppName 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "containerRegistryLoginServer" $containerRegistryLoginServer 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "entraClientId" $entraClientId 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "entraTenantId" $tenantId 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "entraApplicationIdURI" $entraApplicationIdURI 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "resourceGroup" $resourceGroupName 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "siteDeploymentToken" $siteDeploymentToken $true

<# --------------------------------------------------------------------------------- #>
<# Update Build Pipeline #>
Write-Host "Finished"
