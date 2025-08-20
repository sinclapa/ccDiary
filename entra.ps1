$name="ccdiary"
$environment="dev"
$staticSiteUrl="mango-beach-04a5e0e03.5.azurestaticapps.net"
$containerAppUrl="app-ccdiary-dev.greenbush-673f8a47.westeurope.azurecontainerapps.io"
$resourceGroupId="9f7046d3-4948-41cd-9c27-89d0c90186fa"
function New-GuidFromString {
    param([string]$InputString)
    $hasher = [System.Security.Cryptography.MD5]::Create()
    $hashBytes = $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($InputString))
    return [System.Guid]::new($hashBytes)
}


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