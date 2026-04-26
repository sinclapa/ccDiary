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
        },
        @{
            id = (New-GuidFromString "${resourceGroupId}-${AppName}-approle-diary-admin").ToString()
            allowedMemberTypes = @("User")
            displayName = "Diary Administrator"
            description = "Full access to all diaries and user management"
            value = "Diary.Admin"
            isEnabled = $true
        },
        @{
            id = (New-GuidFromString "${resourceGroupId}-${AppName}-approle-diary-contributor").ToString()
            allowedMemberTypes = @("User")
            displayName = "Diary Contributor"
            description = "Can create and edit their own diaries"
            value = "Diary.Contributor"
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
        requiredResourceAccess = @(
            @{
                # Microsoft Graph
                resourceAppId = "00000003-0000-0000-c000-000000000000"
                resourceAccess = @(
                    @{
                        # User.Invite.All (Application permission)
                        id   = "09850681-111b-4a89-9bed-3f2cae46d706"
                        type = "Role"
                    }
                )
            }
        )
    } | ConvertTo-Json -Depth 10

    # Write JSON to temp file
    $tempBodyFile = [System.IO.Path]::GetTempFileName()
    Set-Content -Path $tempBodyFile -Value $requestBody -Encoding UTF8
    
    try {
        if ($objectId) {
            # Azure requires scopes and roles to be disabled before they can be updated or removed.
            # Fetch current state and disable all existing scopes and roles first.
            $existingApp = az ad app show --id $appId | ConvertFrom-Json
            $existingScopes = @($existingApp.api.oauth2PermissionScopes)
            $existingRoles  = @($existingApp.appRoles)

            if ($existingScopes.Count -gt 0 -or $existingRoles.Count -gt 0) {
                foreach ($scope in $existingScopes) { $scope.isEnabled = $false }
                foreach ($role  in $existingRoles)  { $role.isEnabled  = $false }

                $disableBody = @{
                    api      = @{ oauth2PermissionScopes = $existingScopes }
                    appRoles = $existingRoles
                } | ConvertTo-Json -Depth 10

                $tempDisableFile = [System.IO.Path]::GetTempFileName()
                try {
                    Set-Content -Path $tempDisableFile -Value $disableBody -Encoding UTF8
                    Get-Content -Path $tempDisableFile | az rest --method PATCH `
                        --uri "https://graph.microsoft.com/v1.0/applications/$objectId" `
                        --headers "Content-Type=application/json" `
                        --body "@-" 2>&1 | Out-Null
                } finally {
                    Remove-Item -Path $tempDisableFile -Force -ErrorAction SilentlyContinue
                }
            }

            # Apply the full update with the desired scopes and roles
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

    # Grant admin consent for User.Invite.All on Microsoft Graph
    Write-Host "  Granting User.Invite.All consent to service principal..."
    $graphSpId = az ad sp show --id "00000003-0000-0000-c000-000000000000" --query "id" -o tsv 2>$null
    if ($graphSpId) {
        $userInviteAllRoleId = "09850681-111b-4a89-9bed-3f2cae46d706"
        $existingGrant = az rest --method GET `
            --uri "https://graph.microsoft.com/v1.0/servicePrincipals/${spId}/appRoleAssignments" `
            --output json | ConvertFrom-Json
        $alreadyGranted = $existingGrant.value | Where-Object { $_.appRoleId -eq $userInviteAllRoleId -and $_.resourceId -eq $graphSpId }
        if (-not $alreadyGranted) {
            $grantBody = @{
                principalId = $spId
                resourceId  = $graphSpId
                appRoleId   = $userInviteAllRoleId
            } | ConvertTo-Json
            $tempGrantFile = [System.IO.Path]::GetTempFileName()
            try {
                Set-Content -Path $tempGrantFile -Value $grantBody -Encoding UTF8
                Get-Content -Path $tempGrantFile | az rest --method POST `
                    --uri "https://graph.microsoft.com/v1.0/servicePrincipals/${spId}/appRoleAssignments" `
                    --headers "Content-Type=application/json" `
                    --body "@-" 2>&1 | Out-Null
                Write-Host "  User.Invite.All granted."
            } finally {
                Remove-Item -Path $tempGrantFile -Force -ErrorAction SilentlyContinue
            }
        } else {
            Write-Host "  User.Invite.All already granted."
        }
    } else {
        Write-Host "  WARNING: Could not find Microsoft Graph service principal - skipping consent grant." -ForegroundColor Yellow
    }

    # Create a client secret for the app (used by the API to call Graph)
    Write-Host "  Creating client secret for Graph API access..."

    # Entra allows a maximum of 2 secrets per app; remove the oldest if already at the limit
    $existingSecrets = az ad app credential list --id $appId --output json | ConvertFrom-Json
    if ($existingSecrets.Count -ge 2) {
        $oldest = $existingSecrets | Sort-Object -Property endDateTime | Select-Object -First 1
        Write-Host "  Secret limit reached — removing oldest secret ($($oldest.displayName))..."
        az ad app credential delete --id $appId --key-id $oldest.keyId
    }

    $secretBody = @{
        passwordCredential = @{
            displayName = "Local Dev - $(Get-Date -Format 'yyyy-MM-dd')"
        }
    } | ConvertTo-Json
    $tempSecretFile = [System.IO.Path]::GetTempFileName()
    $clientSecret = $null
    try {
        Set-Content -Path $tempSecretFile -Value $secretBody -Encoding UTF8
        $secretResult = Get-Content -Path $tempSecretFile | az rest --method POST `
            --uri "https://graph.microsoft.com/v1.0/applications/$objectId/addPassword" `
            --headers "Content-Type=application/json" `
            --body "@-" | ConvertFrom-Json
        $clientSecret = $secretResult.secretText
        Write-Host "  Client secret created."
    } finally {
        Remove-Item -Path $tempSecretFile -Force -ErrorAction SilentlyContinue
    }

    $EntraObjectId = $objectId
    $EntraApplicationIdURI = "api://${appId}"
    $EntraClientId = $appId
    Write-Host "  EntraApplicationIdURI = $EntraApplicationIdURI"
    Write-Host "  EntraClientId = $EntraClientId"
    Write-Host "  EntraObjectId = $EntraObjectId"

    return [PSCustomObject]@{
        EntraApplicationIdURI = $EntraApplicationIdURI
        EntraClientId         = $EntraClientId
        EntraObjectId         = $EntraObjectId
        ClientSecret          = $clientSecret
    }
} catch {
    Write-Error "Failed to setup Entra application: $($_.Exception.Message)"
    exit 1
}