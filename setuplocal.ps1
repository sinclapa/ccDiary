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

Write-Host "Configuring Entra App Registration..." -ForegroundColor Cyan
$entraOut = & ".\entraSetup.ps1" `
    -AppName "ccdiary-local-$env:COMPUTERNAME" `
    -spaUris @("https://localhost:54629/swagger/oauth2-redirect.html", "http://localhost:8080/") `
    -webUris @("https://localhost:54629/") `
    -resourceGroupId $env:COMPUTERNAME
$entraClientId = $entraOut.EntraClientId
$entraApplicationIdURI = $entraOut.EntraApplicationIdURI

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
<# Cleanup #>
az logout

<# --------------------------------------------------------------------------------- #>
<# Update Build Pipeline #>
Write-Host "Finished"