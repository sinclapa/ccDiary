# About
This project is for experimenting with writing deo to deploy to Azure. The application consists of a .NET rest service and a Vue UI with data persisted in Microsoft SQL Server. The goal is to have a strong CI/CD pipeline while fitting into the free Azure offering.

# Development Environment Pre-Requisites
It is recommended to install the following applications
* Visual Studio Community (https://visualstudio.microsoft.com/vs/community/)
* Visual Studio Code (https://code.visualstudio.com/)
* SQL Management Studio (https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms?view=sql-server-ver16)
* GIT Bash (https://git-scm.com/downloads)
* Node.js (https://nodejs.org/en)
* Azure CLI (https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows?tabs=azure-cli)
* Docker (https://www.docker.com/get-started/)
* Code (https://github.com/sinclapa/ccDiary)
* GitHub CLI (https://cli.github.com/)

# Setup Azure
## Build Infrastructure
1. Login to Azure
   ```bash
   Connect-AzAccount
   ```
2. Get UserPincipalName and Id by running below. These are needed for following step providing adminUser (UserPincipalName) and adminUserSID (Id). 
    ```bash
    Get-AzADUser
    ```
3. Deploy infrastructure with initial setup.
    ```bash
    New-AzSubscriptionDeployment -Location westeurope -TemplateFile .\deploy\main.bicep
    ```
    > [!NOTE]
    > adminUser is set to UserPrincipalName and adminUserSID is set to Id
4. Take note of the following values
   * resourceGroupName
   * containerAppName
   * containerAppUrl
   * containerRegistryName
   * containerRegistryLoginServer
   * databaseServer
   * databaseName
   * staticSiteName
   * staticSiteUrl
5. Get the Static Web App Deployment Token
   ```bash
   az staticwebapp secrets list --resource-group <resourceGroupName> --name <staticSiteName> --query properties.apiKey
   ```

> [!TIP]
> To make it easier to run New-AzSubscriptionDeployment a parameter file can be created here **.\deploy\main.bicepparam** with following content
> ```
> using './main.bicep'
> 
> param name = '<Name of application>'
> param environment = '<Environment>>'
> param adminUser = '<adminUser>'
> param adminUserSID = '<adminUserSID>'
> ```
> Then run the following command
> ```bash
> New-AzSubscriptionDeployment -Location westeurope -TemplateFile .\deploy\main.bicep -TemplateParameterFile .\deploy\main.bicepparam
> ```

> [!TIP]
> See this guide for extracting output into PowerShell variables https://yobyot.com/powershell/azure-arm-templates-powershell/2019/11/05/ 

## Setup Microsfot Entra Id
1. Go to https://portal.azure.com/#home and select Azure service **Microsfot Entra ID**
2. Open **Manage | App registrations**
3. Click **New registration**
4. Provide Name and select **Supported account types** to be "Accounts in this organizational directory only (Default Directory only - Single tenant)"
5. Click **Register**
6. Go to **Manage | Authentication**
7. Press **Add a platform**
   1. Select **Web**
   2. Enter the following redirect URI "https://localhost:54629/"
   3. Add extra redirect URIs based on infrastructure "{Azure Container App}"
8. Press **Add a platform**
   1. Select **Single-page application**
   2. Enter the following redirect URI "http://localhost:8080/" 
   3. Add extra redirect URIs for "https://localhost:54629/swagger/oauth2-redirect.html" and based on infrastructure "{Azure Static App}" and "{Azure Container App}/swagger/oauth2-redirect.html" 
9. Go to **Manage | Expose an API**
10. Select **Add** next to **Application ID URI** and press **Save**
11. Click on **Add a scope**
    1.  Scope name = Diary.Update
    2.  Who can consent? = Admins only
    3.  Admin consent display name = Update diary details
    4.  Admin consent description = Update diary details within the ccDiary API
    5.  Press **Add scope**
12. Go to **Manage | Add owners**
    1.  Select your account
13. Take note of the following values from **Overview**
    * Application (client) ID
    * Directory (tenant) ID

## Setup BuildPipeline
1. Go to https://dev.azure.com and add **New project**
2. Open **Project settings**
3. Open **Pipeline | Service connections**
4. Click on **Create service connection**
5. Select **Docker Registry** and press **Next**
6. Select Registry Type **Azure Container Registry**
7. Set Authentication Type to **Service Principal**
8. Select your Azure Subscription
9. Select the **Azure container registry** that was output from deploying the infrastructure (containerRegistryName)
10. Set **Service connection name** to "azure-container-registry"
11. Check **Grant access permission to all pipelines**
12. Select Pipelines from the left menu
13. Click on **New service connection**
14. Select **Azure Resource Manager** and press **Next**
15. Select **Workload Identity federation (automatic)** and press **Next**
16. Set **Service connection name** to "Azure Subscription"
17. Check **Grant access permission to all pipelines**
18. Click on **Create Pipeline**
19. Select **Bitbucket Cloud**
20. Select your repository
21. Add the following variables
    |Name                        |Secret|Value                                                                                                       |
    |----------------------------|------|------------------------------------------------------------------------------------------------------------|
    |containerAppName            |N     |Output containerAppName from building infrastructure                                                        | 
    |containerRegistryLoginServer|N     |Output containerRegistryLoginServer from building infrastructure                                            |
    |resourceGroup               |N     |Output resourceGroupName from building infrastructure                                                       |
    |siteDeploymentToken         |Y     |**Static Web App Deployment Token** from building infrastructure                                            |
    |entraClientId               |N     |Extracted Microsoft Entra Id setup as **Application (client) ID**                                           |
    |entraTenantId               |N     |Extracted Microsoft Entra Id setup as **Directory (tenant) ID**                                             |
    
22. Click **Run**
23. Modify **src\ui\.env.production** to point at correct URL for the Container App
24. On the database that has been created run the following SQL with your container app name
    ```SQL
    CREATE USER <containerAppName> FROM EXTERNAL PROVIDER;
    ALTER ROLE db_datareader ADD MEMBER <containerAppName>;
    ALTER ROLE db_datawriter ADD MEMBER <containerAppName>;
    ALTER ROLE db_ddladmin ADD MEMBER <containerAppName>;
    GO
    ```

## Setup GitHub Bot User (Optional)
By default, the repository uses the built-in `github-actions[bot]` user for automated commits (e.g., version bumps). If you prefer to use a dedicated bot account for better tracking and control, follow these steps:

### 1. Create a GitHub Bot Account
1. Sign out of your personal GitHub account
2. Go to https://github.com/signup
3. Create a new GitHub account with a descriptive name (e.g., `ccDiary-bot` or `<yourorg>-automation`)
4. Complete the account setup and verify the email address
5. Add a profile picture and description to make it clear this is a bot account

> [!TIP]
> Consider using a naming convention like `<project>-bot` or `<org>-automation` to make the bot's purpose clear.

### 2. Generate a Personal Access Token (PAT)
1. While logged in as the bot user, go to **Settings** → **Developer settings** → **Personal access tokens** → **Tokens (classic)**
2. Click **Generate new token** → **Generate new token (classic)**
3. Give the token a descriptive name (e.g., "ccDiary Workflow Token")
4. Set an expiration date (or select "No expiration" if your security policy allows)
5. Select the following scopes:
   * `repo` (Full control of private repositories) - Required for pushing commits and tags
   * `workflow` (Update GitHub Actions workflows) - Required if the bot needs to trigger workflows
6. Click **Generate token**
7. **IMPORTANT**: Copy the token immediately - you won't be able to see it again

> [!WARNING]
> Store the token securely. Anyone with this token has the same permissions as the bot account.

### 3. Add Bot as Repository Collaborator
1. Log back in with your personal/organization account
2. Go to the repository **Settings** → **Collaborators and teams**
3. Click **Add people**
4. Search for and add the bot account you created
5. Grant **Write** or **Admin** permissions (Write is sufficient for most automation tasks)
6. The bot account will need to accept the invitation (check the bot account's email or notifications)

### 4. Configure Repository Secrets
1. In your repository, go to **Settings** → **Secrets and variables** → **Actions**
2. Click **New repository secret**
3. Create a secret named `BOT_TOKEN` (or `BOT_PAT`)
4. Paste the Personal Access Token you generated earlier
5. Click **Add secret**

### 5. Update Workflows to Use Bot Token
Update the workflows that need to push commits (like `version-on-commit.yml`) to use the bot token:

```yaml
- name: Checkout
  uses: actions/checkout@v5
  with:
    fetch-depth: 0
    token: ${{ secrets.BOT_TOKEN }}  # Use bot token instead of default GITHUB_TOKEN

# ... other steps ...

- name: Commit and tag version changes
  env:
    NEW_VERSION: ${{ steps.semver.outputs.next_version }}
  run: |
    git config user.name "ccDiary-bot"  # Use your bot's username
    git config user.email "bot@example.com"  # Use your bot's email
    
    git add VERSION src/ui/package.json src/ui/package-lock.json
    git commit -m "chore(version): bump to ${NEW_VERSION} [skip version]"
    git tag "v${NEW_VERSION}"
    
    git push
    git push --tags
```

> [!NOTE]
> The key differences when using a bot token:
> * Add `token: ${{ secrets.BOT_TOKEN }}` to the checkout step
> * Configure git with the bot's username and email
> * The commits will appear as being made by your bot account instead of `github-actions[bot]`

### 6. Benefits of Using a Dedicated Bot Account
* **Better tracking**: See exactly which commits were made by automation vs. team members
* **More control**: Manage bot permissions independently from workflow permissions
* **Easier auditing**: Filter and search for bot activity across repositories
* **Custom identity**: Give your automation a personality with a custom avatar and name
* **Cross-repository automation**: Use the same bot account across multiple repositories in your organization

### 7. Security Best Practices
* Rotate bot tokens regularly (every 90 days recommended)
* Use the minimum required token scopes
* Never commit tokens to the repository
* Consider using fine-grained personal access tokens for better security
* Enable 2FA on the bot account
* Monitor bot account activity regularly

# Running Local Development 
## Initial Setup
Set password for database in user-secrets
```
cd src\api\ccDiaryAPI\
dotnet user-secrets set "SA_PASSWORD" "<password>"
dotnet user-secrets set "Entra:ClientId" "<Application (client) ID>"
dotnet user-secrets set "Entra:TenantId" "<Directory (tenant) ID>"
```

Create .env file to hold database password for docker-compose in .\src\api
```
DB_PASSWORD=<password>
```

Create **.env.dev.local** file in .\src\ui
```
VITE_CLIENT_ID="<Application (client) ID>"
VITE_TENANT_ID="<Directory (tenant) ID>"
```

## Running 
Start the API
```bash
docker compose --project-directory src\API up
```
Start the UI
```bash
npm run dev --prefix src\UI
```
