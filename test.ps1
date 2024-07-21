function SetDevOpsPipelineVariable {
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline)]
        [PSCustomObject]$HashTable,
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
    if ($HashTable.PSObject.Properties.Name.Contains($Name)) {
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

SetDevOpsPipelineVariable $pipelineVariables $devOpsOrg $devOpsProject $devOpsPipelineName "test" "PJS" $true