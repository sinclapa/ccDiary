# Claude Code Context for ccDiary Repository

This document provides essential context for Claude Code instances to effectively work with the ccDiary codebase.

## Project Overview

**ccDiary** is a full-stack diary application that allows users to create, manage, and view diary entries with modern cloud-native architecture.

### Key Characteristics
- **Type**: Full-stack web application
- **Tier**: SaaS with authentication and cloud deployment
- **Purpose**: Diary entry management with Microsoft Entra ID OAuth integration
- **Maturity Level**: Active development (infrastructure automation in progress)
- **Deployment Target**: Azure Container Apps via Bicep IaC

---

## Technology Stack

### Backend
- **Framework**: ASP.NET Core 8 (target framework: net8.0)
- **Architecture**: RESTful API with API versioning (Asp.Versioning)
- **Database**: SQL Server with Entity Framework Core 9.0
- **Authentication**: Microsoft Identity Web (OAuth via Microsoft Entra ID)
- **Logging**: Serilog with structured logging
- **API Docs**: Swagger/OpenAPI with custom authorization filter
- **Monitoring**: Steeltoe Management endpoints (Health, Info, Metrics)
- **Code Quality**: StyleCop.Analyzers, .NET Analyzers (Recommended mode)
- **Package Lock**: Uses packages.lock.json (RestorePackagesWithLockFile enabled)

### Frontend
- **Framework**: Vue.js 3 (Composition API)
- **Styling**: Vuetify 3 (Material Design)
- **Build Tool**: Vite
- **Language**: TypeScript
- **State Management**: Pinia
- **Router**: unplugin-vue-router (type-safe route generation)
- **Authentication**: @azure/msal-browser (Microsoft authentication)
- **Component Auto-Import**: unplugin-vue-components + unplugin-auto-imports
- **Testing**: Vitest with coverage (Istanbul/v8)
- **Linting**: ESLint with TypeScript support

### Infrastructure & DevOps
- **IaC**: Bicep (targeting Azure subscription scope)
- **Containerization**: Docker & Docker Compose
- **Container Registry**: Uses image references for Container Apps
- **CI/CD Launch Points**: PowerShell scripts (setuplocal.ps1, buildInfrastructure.ps1, etc.)
- **Cloud Platform**: Microsoft Azure (Container Apps, SQL Database, Entra ID)

### Development Tools (Recommended)
- Visual Studio Community (for .NET development)
- Visual Studio Code (for TypeScript/Vue, infrastructure as code)
- SQL Server Management Studio (SSMS)
- Azure CLI
- Node.js (for UI development)
- Docker Desktop
- GitHub CLI
- Git Bash

---

## Repository Structure

```
D:\Git\ccDiary/
├── CLAUDE.md                          # This file
├── readme.md                          # User-facing project documentation
├── setuplocal.ps1                     # Interactive local dev environment setup
├── buildInfrastructure.ps1            # Builds Bicep templates and deploys
├── buildAllInfrastructure.ps1         # Full infrastructure build pipeline
├── entraSetup.ps1                     # Microsoft Entra ID configuration
│
├── data/                              # Database initialization & sample data
│   ├── data.sql                       # SQL seed data
│   └── Diary2 - AI.txt               # Sample diary entry
│
├── deploy/                            # Infrastructure as Code (Bicep)
│   ├── main.bicep                    # Root/subscription-level template
│   ├── resourceGroup.bicep           # Resource group module
│   ├── containerApps.bicep           # Container Apps configuration
│   ├── main.json                     # Generated ARM template (from Bicep)
│   └── bicepconfig.json              # Bicep linting configuration
│
├── scripts/                           # Installation scripts
│   ├── install-azure-cli.sh          # Azure CLI installer
│   └── install-powershell.sh         # PowerShell installer
│
└── src/                               # Application source code
    ├── api/                           # Backend API (ASP.NET Core)
    │   ├── ccDiary.sln               # Visual Studio solution file
    │   ├── docker-compose.dcproj     # Docker Compose project
    │   ├── docker-compose.yml        # Compose configuration
    │   ├── docker-compose.override.yml
    │   ├── docker-compose.linux.override.yml
    │   ├── Dockerfile                # API container image
    │   ├── launchSettings.json       # Launch profiles (Local, LocalContainer, etc.)
    │   ├── stylecop.json             # StyleCop rules for code analysis
    │   │
    │   ├── ccDiaryApi/               # Main API project
    │   │   ├── ccDiaryApi.csproj    # .NET project file
    │   │   ├── Program.cs            # Application startup & DI configuration
    │   │   ├── appsettings.json      # Default configuration
    │   │   ├── appsettings.Development.json
    │   │   ├── appsettings.Local.json       # Local development (user secrets)
    │   │   ├── appsettings.LocalContainer.json
    │   │   ├── appsettings.Production.json
    │   │   ├── appsettings.UAT.json
    │   │   ├── packages.lock.json    # Locked dependency versions
    │   │   ├── AuthorizeCheckOperationFilter.cs  # Swagger authorization
    │   │   ├── ConfigureSwaggerOptions.cs         # API documentation setup
    │   │   ├── WeatherForecast.cs    # Sample controller (can be deleted)
    │   │   │
    │   │   ├── Controllers/          # API endpoint handlers
    │   │   │   └── v1/               # API v1 controllers
    │   │   │
    │   │   ├── Data/                 # Data access layer
    │   │   │   ├── Context/          # DbContext and database configuration
    │   │   │   ├── Migration/        # EF Core migrations
    │   │   │   └── Model/            # Entity models (DiaryEntry, etc.)
    │   │   │
    │   │   ├── Services/             # Business logic services
    │   │   ├── Extensions/           # Extension methods (Startup pipeline)
    │   │   ├── Utilities/            # Helper utilities
    │   │   ├── Properties/           # Assembly properties
    │   │   └── Migrations/           # EF Core migration history
    │   │
    │   └── ccDiaryApiTest/           # API integration & unit tests
    │       ├── ccDiaryApiTest.csproj
    │       ├── CustomWebApplicationFactory.cs  # Test web host factory
    │       ├── TestAuthHandler.cs    # Mock authentication
    │       ├── Helpers.cs            # Test utilities
    │       ├── Integration/          # Integration tests
    │       └── v1/                   # API v1 test suites
    │
    └── ui/                            # Frontend Vue.js application
        ├── package.json              # npm dependencies
        ├── tsconfig.json             # TypeScript configuration
        ├── tsconfig.node.json        # TypeScript build tools config
        ├── vite.config.mts           # Vite build configuration (TypeScript)
        ├── index.html                # HTML entry point
        ├── README.md                 # UI-specific documentation
        │
        ├── public/                   # Static assets
        │   ├── config.js             # Runtime configuration (environment)
        │   └── staticwebapp.config.json  # Azure Static Web Apps config
        │
        └── src/                      # Vue source code
            ├── main.ts               # Application entry point
            ├── App.vue               # Root component
            ├── env.d.ts              # Environment variable type definitions
            ├── vite-env.d.ts         # Vite type definitions
            ├── typed-router.d.ts     # Auto-generated router types
            ├── auto-imports.d.ts     # Auto-generated component imports
            ├── components.d.ts       # Auto-generated component registry
            │
            ├── assets/               # Images, fonts, static resources
            ├── components/           # Reusable Vue components
            ├── layouts/              # Layout components (AppLayout, etc.)
            ├── pages/                # Page components (routed)
            ├── router/               # Vue Router configuration
            ├── services/             # API clients & external services
            ├── stores/               # Pinia state management
            ├── styles/               # Global SCSS/CSS
            ├── utils/                # Utility functions
            ├── plugins/              # Vue plugins (Vuetify, etc.)
            │
            └── tests/                # Frontend tests
                ├── setupTests.ts     # Test environment setup
                ├── app.spec.ts       # App component tests
                ├── components/       # Component tests
                ├── pages/            # Page tests
                ├── services/         # Service/API tests
                └── stores/           # State management tests
```

---

## Development Workflow

### Initial Setup (First Time)

```powershell
# 1. Clone the repository
git clone https://github.com/sinclapa/ccdiary.git
cd ccDiary

# 2. Run the automated setup (sets up user secrets, SQL Server, Entra ID)
./setuplocal.ps1

# 3. Verify Azure CLI and credentials are configured
az account show
```

### Running the Backend API

```powershell
# Navigate to API directory
pushd src/api

# Build the solution
dotnet build ccDiary.sln

# Run the API (development mode)
dotnet run --project ccDiaryApi/ccDiaryApi.csproj

# API will be available at:
# - https://localhost:7183/
# - Swagger UI: https://localhost:7183/swagger

# Run tests
dotnet test ccDiaryApiTest/ccDiaryApiTest.csproj

popd
```

### Running the Frontend UI

```powershell
# Navigate to UI directory
cd src/ui

# Install dependencies
npm install

# Start development server
npm run dev

# UI will be available at: http://localhost:8080 (or next available port)

# Run tests with coverage
npm run test
npm run coverage

# Run tests with UI
npm run test:ui

# Lint and fix code
npm run lint

# Build for production
npm run build
```

### Docker Compose (Local Development)

> **Status**: Not fully working yet according to readme

```powershell
pushd src/api
docker-compose up
# Starts SQL Server + API containers
popd
```

---

## Key Configuration Files & Architecture

### Authentication & Secrets

**User Secrets** (`.csproj` configured with `UserSecretsId: cab124e3-5845-4057-8fa9-02c8ad6cc9ba`)
- Stored in: `%APPDATA%\Microsoft\UserSecrets\cab124e3-5845-4057-8fa9-02c8ad6cc9ba\secrets.json`
- Used for: Local development connection strings, Azure credentials
- Set via: `setuplocal.ps1` script or `dotnet user-secrets set`

**Environment Variables**
- `ASPNETCORE_ENVIRONMENT`: Controls which appsettings file loads (Local, LocalContainer, Development, UAT, Production)
- `AZURE_SQL_CONNECTIONSTRING`: SQL Server connection string
- Backend requires valid SQL connection string at startup (throws InvalidOperationException if missing)
- Frontend loads config from `public/config.js` at runtime (injected by deployment)

### Database Configuration

- **EF Core Version**: 9.0.0
- **Provider**: SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`)
- **Migrations Folder**: `ccDiaryApi/Migrations/`
- **DbContext Location**: `ccDiaryApi/Data/Context/`
- **Models Location**: `ccDiaryApi/Data/Model/`

**Create New Migration**:
```powershell
cd src/api
dotnet ef migrations add MigrationName --project ccDiaryApi --startup-project ccDiaryApi
```

**Apply Migrations**:
- Automatic on app startup (configured in Program.cs)
- Manual: `dotnet ef database update --project ccDiaryApi`

### API Structure

**Versioning**: Using `Asp.Versioning` (modern ASP.NET Core API versioning)
- Controllers in: `ccDiaryApi/Controllers/v1/`
- Route pattern: `/api/v{version}/endpoint`
- Current version: v1

**Swagger/OpenAPI**:
- Configured in `ConfigureSwaggerOptions.cs`
- Custom `AuthorizeCheckOperationFilter.cs` adds authorization requirements to Swagger
- Access at: `https://localhost:7183/swagger`

**Testing**:
- Integration tests use `CustomWebApplicationFactory<Program>`
- Mock authentication via `TestAuthHandler` + `TestAuthHandlerOptions`
- Test helpers in `ccDiaryApiTest/Helpers.cs`

### Frontend Build & Type Safety

**Vite Config** (`vite.config.mts`):
- TypeScript as build configuration language
- Vue plugin enabled
- Auto-imports and component generation configured
- `--mode dev` flag for development builds

**Type-Safe Routing**:
- Auto-generated `typed-router.d.ts` from route files
- `unplugin-vue-router` provides file-based routing with types

**Auto-Imports**:
- `unplugin-auto-imports`: Auto-imports common functions (ref, computed, etc.)
- `unplugin-vue-components`: Auto-registers components (no import needed)
- Generated definitions in `auto-imports.d.ts` and `components.d.ts`

### Infrastructure as Code (Bicep)

**File Structure**:
- `main.bicep`: Subscription-level entrypoint (creates resource group, calls modules)
- `resourceGroup.bicep`: Resources within the resource group (Container Apps, SQL, etc.)
- `containerApps.bicep`: Azure Container Apps specific configuration
- `bicepconfig.json`: Linting and analyzer rules for Bicep

**Parameters**:
- `name`: Application prefix (5-20 chars)
- `environment`: Deployment environment (dev, uat, prod)
- `adminUser`: Admin username
- `adminUserSID`: Azure AD user object ID
- `devApiContainerImage`: Container image URI for API
- `externalDomainName`: Custom domain (optional)
- `location`: Azure region (defaults to deployment location)

**Deployment Commands** (see `buildInfrastructure.ps1`):
```powershell
./buildInfrastructure.ps1 -name "ccdiary" -environment "dev"
```

---

## Common Development Tasks

### Add a New API Endpoint

1. Create controller in `src/api/ccDiaryApi/Controllers/v1/`
2. Inherit from `ControllerBase`
3. Apply `[ApiVersion("1.0")]` attribute
4. Add `[Authorize]` if authentication required
5. Map routes via `[HttpGet]`, `[HttpPost]`, etc.
6. Add tests to `src/api/ccDiaryApiTest/v1/`

### Add a New Database Entity

1. Create model class in `ccDiaryApi/Data/Model/`
2. Add `DbSet<YourModel>` to DbContext in `ccDiaryApi/Data/Context/`
3. Create migration:
   ```powershell
   dotnet ef migrations add AddYourModel --project ccDiaryApi
   ```
4. Review migration in `ccDiaryApi/Migrations/`
5. Migration applies automatically on next app start (or use `dotnet ef database update`)

### Add Frontend Component

1. Create `.vue` file in `src/ui/src/components/` or `src/pages/`
2. Use `<script setup lang="ts">` for Composition API
3. Component auto-imports (no import statement needed if using `unplugin-vue-components`)
4. Use Vuetify components: `<v-card>`, `<v-btn>`, etc.

### Handle Authentication

**Backend**:
- Requires `Microsoft.Identity.Web` JWT bearer token validation
- Token from Microsoft Entra ID (OAuth flow)
- Validate with `[Authorize]` attribute
- Access token claims via `User.FindFirst(ClaimTypes.NameIdentifier)`

**Frontend**:
- Use MSAL (`@azure/msal-browser`) for login/token management
- Call API with access token in Authorization header: `Bearer {token}`
- Automatic token refresh via MSAL

### Update Entity Framework Migrations

If making database schema changes:

```powershell
# Add new migration
dotnet ef migrations add DescriptiveNameOfChange --project ccDiaryApi --startup-project ccDiaryApi

# Remove last migration (if not yet applied)
dotnet ef migrations remove --project ccDiaryApi

# Generate migration script for SQL Server
dotnet ef migrations script --project ccDiaryApi
```

---

## Testing

### API Tests

- Framework: xUnit or similar (via ccDiaryApiTest.csproj)
- Location: `src/api/ccDiaryApiTest/`
- Custom factory: `CustomWebApplicationFactory.cs` (in-memory database or test SQL)
- Mock auth: `TestAuthHandler.cs` + `TestAuthHandlerOptions.cs`
- Run: `dotnet test ccDiaryApiTest/ccDiaryApiTest.csproj`

### UI Tests

- Framework: Vitest with Happy DOM / jsdom
- Location: `src/ui/tests/`
- Coverage: Istanbul or v8
- Commands:
  ```powershell
  npm run test              # Run tests
  npm run test:ci          # CI mode (JUnit output)
  npm run coverage         # Generate coverage report
  npm run test:ui          # Interactive test UI
  ```

---

## Deployment & Build Artifacts

### Bicep/Infrastructure Build

```powershell
./buildInfrastructure.ps1 -name "ccdiary" -environment "dev" -region "eastus"
```

- Generates `deploy/main.json` (ARM template)
- Creates/updates Azure resources via `az deployment sub create`
- Deploys Container Apps with API image
- Provisions SQL Server instance
- Configures RBAC and Entra ID access

### API Container Image

- **Dockerfile**: `src/api/Dockerfile`
- **Base Image**: Linux-based (.NET runtime)
- **Build Context**: `src/api/`
- **Registry**: Configured in deployment scripts (likely Azure Container Registry)

### Frontend Static Web App

- **Built Output**: `src/ui/dist/`
- **Build Command**: `npm run build`
- **Config**: `public/staticwebapp.config.json` for Azure Static Web Apps
- **Runtime Config**: `public/config.js` (injected with API endpoint, auth config)

---

## Configuration & Secrets Management

### Local Development (.env / User Secrets)

**Setup via** `setuplocal.ps1`:
- SQL Server connection string
- Azure Entra ID credentials
- API base URLs
- Stored in Windows Credential Manager or User Secrets

### Environments

| Environment       | Use Case                  | Config File                       |
|-------------------|---------------------------|-----------------------------------|
| Local             | Windows local dev         | `appsettings.Local.json`          |
| LocalContainer    | Docker Compose dev        | `appsettings.LocalContainer.json` |
| Development       | Dev server deployment     | `appsettings.Development.json`    |
| UAT               | User acceptance testing   | `appsettings.UAT.json`            |
| Production        | Production deployment     | `appsettings.Production.json`     |

**Select environment**: Set `ASPNETCORE_ENVIRONMENT` environment variable

### Authentication Configuration

**Microsoft Entra ID Setup**:
- Handled by `entraSetup.ps1`
- Requires Azure CLI + admin consent
- Registers application in Azure AD
- Configures OAuth redirect URIs

**Backend Auth Config** (in `appsettings.json`):
```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-app-id"
  }
}
```

**Frontend Auth Config** (in `public/config.js` or environment vars):
```javascript
{
  "tenant": "your-tenant-id",
  "clientId": "your-app-id",
  "redirectUri": "http://localhost:8080"
}
```

---

## Code Quality & Standards

### Backend

- **StyleCop**: Enabled (see `stylecop.json`)
  - Enforces naming conventions, documentation, spacing
  - Warnings treated as errors in some configurations
  - Rules configured in root `stylecop.json`

- **.NET Analyzers**: Enabled in Recommended mode
  - Catches security issues, performance problems
  - Configure in `.csproj`: `<AnalysisMode>Recommended</AnalysisMode>`

- **Code Documentation**: XML comments required
  - `<GenerateDocumentationFile>True</GenerateDocumentationFile>` in `.csproj`
  - Populates Swagger definitions
  - CS1591 warnings suppressed in `<NoWarn>` (optional)

### Frontend

- **TypeScript**: Strict mode enabled
- **ESLint**: Standard config + Vuetify rules
  - Run: `npm run lint`
  - Auto-fixes violations

- **Vitest**: For unit/component testing
  - Istanbul or v8 coverage
  - jsdom/Happy DOM environments

---

## Known Issues & Workarounds

### Docker Compose Not Working Yet

- Status: Incomplete (per readme)
- Workaround: Run API and SQL locally without containers, or set up SQL container separately and point API to it
- Issue likely: Network configuration, SQL Server connection string in docker environment

### API localhost Port

- Default: `https://localhost:7183`
- Configured in: `launchSettings.json`
- May need to accept self-signed cert on first access

### MSAL Browser Azure AD Integration

- Requires proper app registration in Azure AD
- Redirect URIs must match deployment (localhost:8080 for dev, production domain for prod)
- Token scopes must include API permissions configured in app registration
- Run `entraSetup.ps1` to automate this

---

## Git & Repository Info

### Repository

- **Repo**: https://github.com/sinclapa/ccdiary
- **Local Path**: D:\Git\ccDiary
- **Branch Strategy**: Conventional (main for production, feature branches)

### Key Scripts in Root

| Script | Purpose |
|--------|---------|
| `setuplocal.ps1` | One-time local dev environment setup |
| `buildInfrastructure.ps1` | Build Bicep, deploy to Azure |
| `buildAllInfrastructure.ps1` | Full pipeline (build, test, deploy) |
| `entraSetup.ps1` | Configure Microsoft Entra ID |

---

## Helpful Commands Reference

### Common .NET Commands

```powershell
cd src/api

# Build solution
dotnet build ccDiary.sln

# Restore packages
dotnet restore

# Run API
dotnet run --project ccDiaryApi/ccDiaryApi.csproj

# Run tests
dotnet test

# Open test results
# Check test output in ccDiaryApiTest/bin/Debug/net8.0/TestResults/
```

### Common npm Commands

```powershell
cd src/ui

npm install                # Install dependencies
npm run dev               # Start dev server
npm run build             # Production build
npm run lint              # Check & fix code style
npm run test              # Run tests
npm run test:ui           # Interactive test UI
npm run coverage          # Coverage report
```

### Entity Framework Migrations

```powershell
cd src/api

# Add migration
dotnet ef migrations add MigrationName --project ccDiaryApi

# Remove last migration
dotnet ef migrations remove --project ccDiaryApi

# Update database
dotnet ef database update --project ccDiaryApi

# Drop database
dotnet ef database drop --project ccDiaryApi

# Script migrations to SQL
dotnet ef migrations script --project ccDiaryApi > migration.sql
```

### Azure CLI

```powershell
# Login
az login

# Show current subscription
az account show

# List resource groups
az group list

# Deploy infrastructure
az deployment sub create \
  --location eastus \
  --template-file deploy/main.json \
  --parameters name=ccdiary environment=dev
```

---

## Notes for Claude Instances

1. **Read the readme.md first** for user-facing documentation
2. **Check launchSettings.json** for local debugging ports and profiles
3. **Understand User Secrets** are required for local development (setuplocal.ps1 creates them)
4. **API versioning is explicit** - routes include `/api/v1/`
5. **Frontend auto-import system** means components and utilities don't need explicit imports
6. **Authentication is critical** - both ends require Entra ID/OAuth setup
7. **Infrastructure as Code** uses Bicep (not ARM JSON directly) - see `deploy/` folder
8. **Database changes** always require migrations (never manual SQL in production)
9. **Tests are integration-level** for API (full app context via factory) - keep them meaningful
10. **Docker Compose is incomplete** - work around it for now

---

## Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Vue.js 3 Guide](https://vuejs.org/)
- [Vuetify 3 Documentation](https://vuetifyjs.com/)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core)
- [Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Microsoft Entra ID (Azure AD) Docs](https://docs.microsoft.com/en-us/azure/active-directory/)
- [Azure Container Apps](https://docs.microsoft.com/en-us/azure/container-apps/)

---

**Last Updated**: 2026-03-20
**Created For**: Claude Code instances working on ccDiary repository
