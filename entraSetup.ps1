<#
.SYNOPSIS
    Sets up Entra (Azure AD) application registration for the specified app.

.DESCRIPTION
    This script creates or updates an Entra (Azure AD) application registration with the specified name.
    It configures redirect URIs, scopes, and permissions required for the application.

.PARAMETER AppName
    The name of the application to create or update in Entra ID.

.PARAMETER spaUris
    An array of redirect URIs for Single Page Applications (SPAs).

.PARAMETER webUris
    An array of redirect URIs for web applications.

.PARAMETER resourceGroupId
    The resource group ID used for generating unique identifiers.

.OUTPUTS
    A PSCustomObject containing:
    - EntraApplicationIdURI: The Application ID URI of the created/updated Entra application.
    - EntraClientId: The Client ID of the created/updated Entra application.

.EXAMPLE
    .\entraSetup.ps1 -AppName "App-Name" -spaUris @("https://example.com/") -webUris @("https://api.example.com/") -resourceGroupId "/subscriptions/xxxx/resourceGroups/Name"

.NOTES
    Requires Azure CLI to be installed and authenticated.
    Author: Paul Sinclair
    Version: 1.0
#>


[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, 
               Position = 0,
               HelpMessage = "Enter the name of the application")]
    [ValidateNotNullOrEmpty()]
    [string]$AppName,

    [Parameter(Mandatory = $true,
               HelpMessage = "Enter the list of SPA redirect URIs")]
    [string[]]$spaUris,

    [Parameter(Mandatory = $true,
               HelpMessage = "Enter the list of web redirect URIs")]
    [string[]]$webUris,

    [Parameter(Mandatory = $true,
               HelpMessage = "Enter the resource group ID")]
    [ValidateNotNullOrEmpty()]
    [string]$resourceGroupId
)

# Function to generate a GUID from a string
function New-GuidFromString {
    param([string]$InputString)
    $hasher = [System.Security.Cryptography.MD5]::Create()
    $hashBytes = $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($InputString))
    return [System.Guid]::new($hashBytes)
}

# Script logic starts here
Write-Host "Setting up Entra application: $AppName" -ForegroundColor Cyan

# Your script implementation goes here
try {

    $appSpaJson = @{redirectUris = $spaUris} | ConvertTo-Json -Depth 3 -Compress
    $appUpdateBody = $appSpaJson | ConvertTo-Json -Depth 4

    $appId=$(az ad app list --filter "displayName eq '$AppName'" --query "[0].appId" -o tsv)

    if ($appId -ne "" -and $null -ne $appId) {
        Write-Host "  Updating existing application: $appId"
        
        # Update application
        az ad app update --id $appId `
            --web-redirect-uris $webUris `
            --set spa=$appUpdateBody `
            --identifier-uris "api://${appId}" `
            --enable-id-token-issuance true `
            --sign-in-audience AzureADMyOrg
    } else {
        Write-Host "  Creating new application..."
        
        # Create application
        $appId = az ad app create `
            --display-name $AppName `
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
                    id = New-GuidFromString "${resourceGroupId}-${AppName}-oauth2-diary-update"
                    value = "Diary.Update"
                    adminConsentDisplayName = "Update ${AppName} details"
                    adminConsentDescription = "Update ${AppName} details permission"
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
            id = New-GuidFromString "${resourceGroupId}-${AppName}-resourceAccess-scope-00000003-0000-0000-c000-000000000000"
            type = "Scope"
        }
        )    
    }
    )
    $resourceJsonOutput = $resourceJson | ConvertTo-Json -Depth 10 -Compress
    $resourceJsonOutputBody = $resourceJsonOutput | ConvertTo-Json -d 4

    az ad app update --id $appId --set requiredResourceAccess="[$resourceJsonOutputBody]"

    $EntraApplicationIdURI = "api://${appId}"
    $EntraClientId = $appId
    Write-Host "  EntraApplicationIdURI = $EntraApplicationIdURI"
    Write-Host "  EntraClientId = $EntraClientId"
    
    # Return object with 2 string properties
    return [PSCustomObject]@{
        EntraApplicationIdURI = $EntraApplicationIdURI
        EntraClientId = $EntraClientId
    }
} catch {
    Write-Error "Failed to setup Entra application: $($_.Exception.Message)"
    exit 1
}