Connect-AzAccount
$outputUser = Get-AzADUser
$userId = $outputUser.Id
$userPrincipalName = $outputUser.UserPrincipalName

$outputInfrastructure = New-AzSubscriptionDeployment -Location westeurope -TemplateFile .\deploy\main.bicep -nameFromTemplate 'ccdiary' -environment 'dev' -adminUser $userPrincipalName -adminUserSID $userId
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

$devOpsOrg = "https://dev.azure.com/cookingcode/"
$devOpsProject = "ccDiary"
$devOpsPipelineName = "pjsinclair.ccdiary"
$pipelineVariables = az pipelines variable list --org $devOpsOrg --project $devOpsProject --pipeline-name $devOpsPipelineName | ConvertFrom-Json 

$siteDeploymentToken = az staticwebapp secrets list --resource-group $resourceGroupName --name $staticSiteName --query properties.apiKey
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "containerAppName" $containerAppName 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "containerRegistryLoginServer" $containerRegistryLoginServer 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "entraClientId" $entraClientId 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "entraTenantId" $entraTenantId 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "entraApplicationIdURI" $entraApplicationIdURI 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "resourceGroup" $resourceGroupName 
SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "siteDeploymentToken" $siteDeploymentToken $true