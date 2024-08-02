<# --------------------------------------------------------------------------------- #>
<# Setup PowerShell #>

if (-Not (Get-Module -ListAvailable -Name SqlServer)) {
    Install-Module -Name SqlServer -Force
}

<# --------------------------------------------------------------------------------- #>
<# Capture inputs #>
$settingsFile = "buildInfrastructure.settings"

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

$params | ConvertTo-StringData | Set-Content $settingsFile

<# --------------------------------------------------------------------------------- #>
<# Get Azure Params #>
Connect-AzAccount
$outputUser = Get-AzADUser
$userId = $outputUser.Id
$userPrincipalName = $outputUser.UserPrincipalName

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
    if ($key -eq "entraApplicationIdURI") {
        $entraApplicationIdURI = $outputInfrastructure.Outputs[$key].value
    }
    if ($key -eq "entraClientId") {
        $entraClientId = $outputInfrastructure.Outputs[$key].value
    }
    if ($key -eq "entraTenantId") {
        $entraTenantId = $outputInfrastructure.Outputs[$key].value
    }
}

Write-Output "resourceGroupName = $resourceGroupName"
Write-Output "containerAppName = $containerAppName"
Write-Output "containerAppUrl = $containerAppUrl"
Write-Output "containerRegistryName = $containerRegistryName"
Write-Output "containerRegistryLoginServer = $containerRegistryLoginServer"
Write-Output "databaseServer = $databaseServer"
Write-Output "databaseName = $databaseName"
Write-Output "staticSiteName = $staticSiteName"
Write-Output "staticSiteUrl = $staticSiteUrl"
Write-Output "entraApplicationIdURI = $entraApplicationIdURI"
Write-Output "entraClientId = $entraClientId"
Write-Output "entraTenantId = $entraTenantId"

<# --------------------------------------------------------------------------------- #>
<# Update Local Build Environment #>

Write-Host "Updating Local API Build"
dotnet user-secrets -p .\src\api\ccDiaryApi\ccDiaryApi.csproj init
dotnet user-secrets -p .\src\api\ccDiaryApi\ccDiaryApi.csproj set "Entra:TenantId" $entraTenantId 
dotnet user-secrets -p .\src\api\ccDiaryApi\ccDiaryApi.csproj set "Entra:ClientId" $entraClientId
dotnet user-secrets -p .\src\api\ccDiaryApi\ccDiaryApi.csproj set "Entra:ApplicationIdUri" $entraApplicationIdURI

Write-Host "Updating Local UI Build"
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
SetValueInHashTable $content "VITE_TENANTID" """$entraTenantId"""
SetValueInHashTable $content "VITE_APPLICATIONID_URI" """$entraApplicationIdURI"""
$content | ConvertTo-StringData | Set-Content $vuePath

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
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "entraTenantId" $entraTenantId 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "entraApplicationIdURI" $entraApplicationIdURI 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "resourceGroup" $resourceGroupName 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "siteDeploymentToken" $siteDeploymentToken $true

<# --------------------------------------------------------------------------------- #>
<# Update Build Pipeline #>

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