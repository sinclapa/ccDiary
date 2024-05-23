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

# Running Local Development 
## Initial Setup
Set password for database in user-secrets
```
cd src\api\ccDiaryAPI\
dotnet user-secrets set "SA_PASSWORD" "<password>"
```

Create .env file to hold database password for docker-compose in .\src\api
```
DB_PASSWORD=<password>
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

# Setup Azure Pipeline
## Build Infrastructure
1. Login to Azure
   ```bash
   Connect-AzAccount
   ```
2. Get UserPincipalName and Id by running below. These are needed for following step providing adminUser (UserPincipalName) and adminUserSID (Id). 
    ```bash
    Get-AzADUser
    ```

3. Deploy infrastructure with initial setup. This first execution will not deploy the API hosting app as the first image needs to be deployed.
    ```
    New-AzSubscriptionDeployment -Location westeurope -TemplateFile .\deploy\main.bicep
    ```
    > [!NOTE]
    > adminUser is set to UserPrincipalName and adminUserSID is set to Id

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
    |Name                        |Secret|Value|
    |----------------------------|------|---------------------------------------------------------|
    |containerRegistryLoginServer|      |containerRegistryLoginServer from building infrastructure|
    |resourceGroup               |      |resourceGroupName from building infrastructure           |
22. Click **Run**
23. 