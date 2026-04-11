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

    $appRoleId = (New-GuidFromString "${resourceGroupId}-${AppName}-approle-diary-seed").ToString()
    $appRoles = @(
        @{
            id = $appRoleId
            allowedMemberTypes = @("Application")
            displayName = "Seed test data"
            description = "Allows seeding of test data for automated testing (used by CI/CD)"
            value = "Diary.Seed"
            isEnabled = $true
        }
    )

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

    # Get the object ID for the application
    $objectId = az ad app show --id $appId --query "id" -o tsv

    # Build the request body for Graph API
    # Update spa and web redirect URIs, identifier URI, and API scopes
    # isFallbackPublicClient = true allows device code / MSAL public-client flows for local dev tooling
    $requestBody = @{
        identifierUris        = @("api://${appId}")
        isFallbackPublicClient = $true
        spa = @{ redirectUris = $spaUris }
        web = @{
            redirectUris = $webUris
            implicitGrantSettings = @{
                enableIdTokenIssuance  = $true
                enableAccessTokenIssuance = $false
            }
        }
        api      = @{ oauth2PermissionScopes = $oauthScopes }
        appRoles = $appRoles
    } | ConvertTo-Json -Depth 10

    # Write JSON to temp file
    $tempBodyFile = [System.IO.Path]::GetTempFileName()
    Set-Content -Path $tempBodyFile -Value $requestBody -Encoding UTF8
    
    try {
        if ($objectId) {
            # Use Get-Content to read file and pipe to az rest
            Get-Content -Path $tempBodyFile | az rest --method PATCH `
                --uri "https://graph.microsoft.com/v1.0/applications/$objectId" `
                --headers "Content-Type=application/json" `
                --body "@-"
        } else {
            Write-Error "Failed to retrieve application object ID for $AppName"
            exit 1
        }
    } finally {
        Remove-Item -Path $tempBodyFile -Force -ErrorAction SilentlyContinue
    }

    # Ensure service principal exists (auto-created on first app creation, but may lag)
    Write-Host "  Ensuring service principal exists..."
    $spId = az ad sp show --id $appId --query "id" -o tsv 2>$null
    if (-not $spId) {
        Write-Host "  Creating service principal..."
        $spId = az ad sp create --id $appId --query "id" -o tsv
    }

    # Assign Diary.Seed app role to the service principal so client credentials can acquire a token
    Write-Host "  Assigning Diary.Seed app role to service principal..."
    $existingAssignments = az rest --method GET `
        --uri "https://graph.microsoft.com/v1.0/servicePrincipals/${spId}/appRoleAssignments" `
        --output json | ConvertFrom-Json
    $alreadyAssigned = $existingAssignments.value | Where-Object { $_.appRoleId -eq $appRoleId }
    if (-not $alreadyAssigned) {
        $roleAssignmentBody = @{
            principalId = $spId
            resourceId = $spId
            appRoleId = $appRoleId
        } | ConvertTo-Json
        $tempRoleFile = [System.IO.Path]::GetTempFileName()
        try {
            Set-Content -Path $tempRoleFile -Value $roleAssignmentBody -Encoding UTF8
            Get-Content -Path $tempRoleFile | az rest --method POST `
                --uri "https://graph.microsoft.com/v1.0/servicePrincipals/${spId}/appRoleAssignments" `
                --headers "Content-Type=application/json" `
                --body "@-"
        } finally {
            Remove-Item -Path $tempRoleFile -Force -ErrorAction SilentlyContinue
        }
        Write-Host "  Diary.Seed role assigned."
    } else {
        Write-Host "  Diary.Seed role already assigned."
    }

    $EntraObjectId = $objectId
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