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
    - EntraObjectId: The Object ID of the created/updated Entra application.

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

# Script logic starts here
Write-Host "Setting up Entra application: $AppName" -ForegroundColor Cyan

# Your script implementation goes here
try {
    $appId=$(az ad app list --filter "displayName eq '$AppName'" --query "[0].appId" -o tsv)

    $spaJson = @{ redirectUris = $spaUris } | ConvertTo-Json -Depth 5 -Compress
    $webJson = @{ 
        redirectUris = $webUris
        implicitGrantSettings = @{
            enableIdTokenIssuance = $true
            enableAccessTokenIssuance = $false
        }
    } | ConvertTo-Json -Depth 8 -Compress

    $oauthScopes = @(
        @{
            id = (New-GuidFromString "${resourceGroupId}-${AppName}-oauth2-diary-update").ToString()
            value = "Diary.Update"
            adminConsentDisplayName = "Update ${AppName} details"
            adminConsentDescription = "Update ${AppName} details permission"
            userConsentDescription = "Allow the app to update ${AppName} details on your behalf"
            userConsentDisplayName = "Update ${AppName} details"
            type = "User"
            isEnabled = $true
        }
    )
    $apiJson = @{ oauth2PermissionScopes = $oauthScopes } | ConvertTo-Json -Depth 10 -Compress

    if ($appId -ne "" -and $null -ne $appId) {
        Write-Host "  Updating existing application: $appId"
    } else {
        Write-Host "  Creating new application..."

        # Create application
        $appId = az ad app create `
            --display-name $AppName `
            --sign-in-audience AzureADMyOrg `
            --query "appId" -o tsv
    }

    az ad app update --id $appId `
        --identifier-uris "api://${appId}" `
        --enable-id-token-issuance true `
        --sign-in-audience AzureADMyOrg `
        --set "spa=$spaJson" "web=$webJson" "api=$apiJson"

    $EntraObjectId = az ad app list --filter "displayName eq '$AppName'" --query "[0].id" -o tsv
    $EntraApplicationIdURI = "api://${appId}"
    $EntraClientId = $appId
    Write-Host "  EntraApplicationIdURI = $EntraApplicationIdURI"
    Write-Host "  EntraClientId = $EntraClientId"
    Write-Host "  EntraObjectId = $EntraObjectId"

    # Return object with 2 string properties
    return [PSCustomObject]@{
        EntraApplicationIdURI = $EntraApplicationIdURI
        EntraClientId = $EntraClientId
        EntraObjectId = $EntraObjectId
    }
} catch {
    Write-Error "Failed to setup Entra application: $($_.Exception.Message)"
    exit 1
}