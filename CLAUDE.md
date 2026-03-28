# Claude Code Context for ccDiary Repository

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ccDiary is a full-stack diary application that allows users to create, manage, and view diary entries with modern
cloud-native architecture. The seed data contains a WW1 diary (Sapper Arthur Carpenter, 1918-1919).

### Key Characteristics

- Type: Full-stack web application
- Tier: SaaS with authentication and cloud deployment
- Purpose: Diary entry management with Microsoft Entra ID OAuth integration
- Maturity Level: Active development (infrastructure automation in progress)
- Deployment Target: Azure Container Apps via Bicep IaC

## Technology Stack

### Backend

- Framework: ASP.NET Core 8 (target framework: net8.0)
- Architecture: RESTful API with API versioning (Asp.Versioning 8.1.0, URL segment-based)
- Database: SQL Server with Entity Framework Core 9.0 (SqlServer + InMemory/Sqlite for tests)
- Authentication: Microsoft Identity Web 4.3.0 (JWT Bearer via Microsoft Entra ID, config section "Entra")
- Logging: Serilog 8.0.3 (Console + Debug sinks, structured logging)
- API Docs: Swashbuckle.AspNetCore 7.0.0 with AuthorizeCheckOperationFilter
- Monitoring: Steeltoe Management 3.2.8 (Health, Info, Metrics actuators at `/actuator`)
- Code Quality: StyleCop.Analyzers 1.1.118, .NET Analyzers (AnalysisMode: Recommended)
- Package Lock: Uses packages.lock.json (RestorePackagesWithLockFile enabled)
- Description: "Cooking Code Diary App" (AssemblyVersion 1.0.0.0)

### Frontend

- Framework: Vue.js 3 (^3.4.31, Composition API)
- Styling: Vuetify 3 (^3.10.4, Material Design with @mdi/font icons)
- Build Tool: Vite 5
- Language: TypeScript 5.4+
- State Management: Pinia ^2.1.7
- Router: unplugin-vue-router ^0.10.0 (type-safe file-based routing)
- Layouts: vite-plugin-vue-layouts ^0.11.0
- Authentication: @azure/msal-browser ^3.21.0
- Component Auto-Import: unplugin-vue-components ^0.27.2 + unplugin-auto-import ^0.17.6
- Date handling: dayjs ^1.11.13
- Fonts: Google Fonts Roboto (via unplugin-fonts), roboto-fontface
- Testing: Vitest ^2.1.3 with happy-dom (default), jsdom available; coverage via v8 + Istanbul
- Linting: ESLint 8 with vue + TypeScript + vuetify configs
- CSS: Sass 1.77.6

### Infrastructure & DevOps

- IaC: Bicep (targeting Azure subscription scope)
- Containerization: Docker (aspnet:8.0-bookworm-slim) & Docker Compose (SQL Server 2022 + API)
- Container Registry: Uses image references for Container Apps
- CI/CD: PowerShell scripts + GitHub Actions (secrets/variables configured by buildInfrastructure.ps1)
- Cloud Platform: Microsoft Azure (Container Apps, SQL Database serverless, Static Web Apps, Entra ID)

### Development Tools (Recommended)

- Visual Studio Community (for .NET development)
- Visual Studio Code (for TypeScript/Vue, infrastructure as code)
- SQL Server Management Studio (SSMS)
- Azure CLI (with containerapp + serviceconnector-passwordless extensions)
- Node.js (for UI development)
- Docker Desktop
- GitHub CLI
- Git Bash

## Repository Structure

```
ccDiary/
├── CLAUDE.md                          # This file
├── readme.md                          # User-facing project documentation
│
├── data/                              # Database initialization & sample data
│   ├── data.sql                       # SQL seed data (WW1 diary, 158 entries, MERGE upserts)
│   └── Diary2 - AI.txt               # Sample diary entry
│
├── deploy/                            # Infrastructure as Code (Bicep)
│   ├── main.bicep                     # Subscription-level entrypoint
│   ├── resourceGroup.bicep            # Resource group resources (Log Analytics, SQL, Static Web App, CAE)
│   ├── containerApps.bicep            # Container App definition (0.25 CPU, 0.5Gi, 0-1 replicas)
│   ├── main.json                      # Generated ARM template (from Bicep)
│   └── bicepconfig.json               # Bicep linting (all core rules set to error)
│
├── scripts/                           # Setup and deployment scripts
│   ├── setuplocal.ps1                 # Interactive local dev environment setup
│   ├── buildInfrastructure.ps1        # Deploys Azure infra for one environment
│   ├── buildAllInfrastructure.ps1     # Orchestrates deploy across all environments
│   ├── entraSetup.ps1                 # Microsoft Entra ID app registration
│   ├── install-azure-cli.sh           # Azure CLI installer
│   └── install-powershell.sh          # PowerShell installer
│
└── src/                               # Application source code
    ├── api/                           # Backend API (ASP.NET Core)
    │   ├── ccDiary.sln                # Visual Studio solution file
    │   ├── docker-compose.dcproj      # Docker Compose project
    │   ├── docker-compose.yml         # Compose: SQL Server 2022 (port 51433) + API
    │   ├── docker-compose.override.yml          # Windows overrides (HTTPS on 54629)
    │   ├── docker-compose.linux.override.yml    # Linux/Codespaces overrides (no HTTPS)
    │   ├── Dockerfile                 # API container (requires pre-built publish output)
    │   ├── launchSettings.json        # Docker Compose launch profile
    │   ├── stylecop.json              # StyleCop rules for code analysis
    │   │
    │   ├── ccDiaryApi/                # Main API project
    │   │   ├── ccDiaryApi.csproj      # .NET project file (net8.0, nullable enabled)
    │   │   ├── Program.cs             # Startup: DI, auth, EF, Serilog, Steeltoe, CORS, Swagger
    │   │   ├── appsettings.json       # Default config (Entra placeholders, Serilog, Steeltoe)
    │   │   ├── appsettings.Development.json
    │   │   ├── appsettings.Local.json          # Local dev (user secrets)
    │   │   ├── appsettings.LocalContainer.json
    │   │   ├── appsettings.Production.json
    │   │   ├── appsettings.UAT.json
    │   │   ├── packages.lock.json     # Locked dependency versions
    │   │   ├── AuthorizeCheckOperationFilter.cs  # Swagger lock icon for [Authorize]
    │   │   ├── ConfigureSwaggerOptions.cs         # Versioned Swagger docs
    │   │   ├── WeatherForecast.cs     # Sample model (unused, can be deleted)
    │   │   │
    │   │   ├── Controllers/v1/        # API v1 controllers
    │   │   │   ├── DiaryController.cs           # CRUD for diaries (Get: AllowAnonymous, CUD: Authorized)
    │   │   │   ├── DiaryEntryController.cs      # CRUD for diary entries
    │   │   │   ├── DiaryArchiveController.cs    # Archive operations (diary + entries bundle)
    │   │   │   └── WeatherForecastController.cs # Sample (unused, can be deleted)
    │   │   │
    │   │   ├── Data/
    │   │   │   ├── Context/
    │   │   │   │   ├── DiaryDatabaseContext.cs  # DbContext (DbSets: Diaries, DiaryEntries)
    │   │   │   │   └── UtcValueConverter.cs     # DateTime → UTC value converter
    │   │   │   └── Model/
    │   │   │       ├── DiaryDTO.cs              # Diary entity (DiaryId, Title, Author, Description)
    │   │   │       ├── DiaryEntryDTO.cs         # Entry entity (DiaryEntryId, Date, Location, Entry, DiaryId FK)
    │   │   │       └── DiaryArchiveDTO.cs       # Composite: Diary + List<DiaryEntry>
    │   │   │
    │   │   ├── Services/
    │   │   │   ├── IDiaryService.cs / DiaryService.cs           # Diary CRUD (Scoped DI)
    │   │   │   ├── IDiaryEntryService.cs / DiaryEntryService.cs # Entry CRUD (Scoped DI)
    │   │   │   ├── IDiaryArchiveService.cs / DiaryArchiveService.cs # Archive ops (Scoped DI)
    │   │   │   ├── DiaryDateRange.cs            # Date range query helper
    │   │   │   └── SearchType.cs                # Search type enum
    │   │   │
    │   │   ├── Extensions/
    │   │   │   └── ApplicationBuilderExtension.cs  # app.MigrateDatabase() (auto-migrate on startup)
    │   │   │
    │   │   ├── Utilities/
    │   │   │   └── AssemblyVersionInfo.cs       # Version information helper
    │   │   │
    │   │   ├── Migrations/                      # EF Core migrations
    │   │   │   ├── 20240523191227_InitialCreate.cs
    │   │   │   ├── 20240527202604_AddTitleAndAuthorToDiar.cs
    │   │   │   ├── 20240527210247_AddDiaryAndMadeLocationAndEntryRequiredToDiaryEntry.cs
    │   │   │   └── DiaryDatabaseContextModelSnapshot.cs
    │   │   │
    │   │   └── Properties/                      # Assembly properties
    │   │
    │   └── ccDiaryApiTest/                      # API tests (MSTest framework)
    │       ├── ccDiaryApiTest.csproj             # MSTest 3.6.3, Moq 4.20.72, coverlet
    │       ├── CustomWebApplicationFactory.cs    # SQLite in-memory + TestAuthHandler
    │       ├── TestAuthHandler.cs                # Custom auth scheme for testing
    │       ├── TestAuthHandlerOptions.cs         # Test auth config
    │       ├── Helpers.cs                        # Test utilities
    │       ├── Integration/
    │       │   ├── DiaryIntegrationTest.cs
    │       │   ├── DiaryEntryIntegrationTest.cs
    │       │   └── DiaryArchiveIntegrationTests.cs
    │       └── v1/
    │           ├── DiaryControllerTest.cs
    │           ├── DiaryEntryControllerTest.cs
    │           ├── DiaryArchiveControllerTest.cs
    │           └── WeatherForecastControllerTest.cs
    │
    └── ui/                                      # Frontend Vue.js application
        ├── package.json                         # npm dependencies & scripts
        ├── tsconfig.json                        # TypeScript config (ESNext, strict, bundler resolution)
        ├── tsconfig.node.json                   # TypeScript build tools config
        ├── vite.config.mts                      # Vite config (plugins, port 8080, vitest setup)
        ├── index.html                           # HTML entry point
        ├── README.md                            # UI-specific documentation
        │
        ├── public/
        │   ├── config.js                        # Runtime configuration (environment)
        │   └── staticwebapp.config.json         # Azure Static Web Apps config
        │
        └── src/
            ├── main.ts                          # App entry: createApp + registerPlugins + mount
            ├── App.vue                          # Root component (v-app > v-main > router-view)
            ├── env.d.ts                         # Environment variable types
            ├── vite-env.d.ts                    # Vite type definitions
            ├── typed-router.d.ts                # Auto-generated router types
            ├── auto-imports.d.ts                # Auto-generated imports (vue, vue-router)
            ├── components.d.ts                  # Auto-generated component registry
            │
            ├── components/
            │   ├── AppHeader.vue                # Application header
            │   ├── AppFooter.vue                # Application footer
            │   ├── DiaryEditor.vue              # Diary create/edit form
            │   └── DiaryEntryEditor.vue         # Diary entry create/edit form
            │
            ├── pages/                           # File-based routing (unplugin-vue-router)
            │   ├── index.vue                    # Home page
            │   └── diaries/
            │       ├── index.vue                # Diaries list page
            │       └── [id].vue                 # Diary detail page (dynamic route)
            │
            ├── services/
            │   ├── authentication/
            │   │   ├── msalConfig.ts            # MSAL configuration
            │   │   └── msalService.ts           # Auth service (login, token acquisition)
            │   ├── models/
            │   │   ├── diary.ts                 # Diary TypeScript interface
            │   │   └── diaryEntry.ts            # DiaryEntry TypeScript interface
            │   └── modules/
            │       ├── diaryService.ts           # Diary API client (CRUD, calls /api/v1/Diary/)
            │       └── diaryEntryService.ts      # DiaryEntry API client
            │
            ├── stores/
            │   ├── app.ts                       # Main Pinia store
            │   └── index.ts                     # Store exports
            │
            ├── layouts/
            │   └── default.vue                  # Default layout wrapper
            │
            ├── plugins/
            │   ├── index.ts                     # Plugin registration (router, pinia, vuetify)
            │   └── vuetify.ts                   # Vuetify configuration
            │
            ├── router/
            │   └── index.ts                     # Vue Router configuration
            │
            ├── styles/
            │   └── settings.scss                # Vuetify SCSS settings override
            │
            ├── utils/
            │   ├── appConfig.ts                 # getAppConfigField() for runtime config
            │   ├── browserTheme.ts              # Browser theme detection
            │   └── __tests__/                   # Utility tests
            │
            ├── assets/                          # Images, fonts, static resources
            │
            └── tests/                           # Frontend tests (Vitest + happy-dom)
                ├── setupTests.ts                # Test environment setup
                ├── app.spec.ts                  # App component tests
                ├── components/                  # Component tests
                ├── layouts/                     # Layout tests
                ├── pages/                       # Page tests
                ├── plugins/                     # Plugin tests
                ├── services/                    # Service tests
                └── stores/                      # Store tests
```

## Key API Endpoints & Services

### API Structure

- Versioning: Asp.Versioning 8.1.0 (URL segment-based: `/api/v{version}/`)
- Route pattern: `api/v{version}/{Controller}/{Action}`
- Current version: v1
- CORS: Allows all origins, methods, and headers (policy name: "cors")
- Database migrations: Auto-applied on startup via `app.MigrateDatabase()`

### Controllers (v1)

| Controller | Route Prefix | Key Endpoints |
|---|---|---|
| DiaryController | `api/v1/Diary` | GET (list/single, AllowAnonymous), POST Create, PUT Update, DELETE (Authorized) |
| DiaryEntryController | `api/v1/DiaryEntry` | CRUD for individual diary entries |
| DiaryArchiveController | `api/v1/DiaryArchive` | Bundle operations (Diary + its entries) |
| WeatherForecastController | `api/v1/WeatherForecast` | Sample (unused) |

### Services (Scoped DI)

- `IDiaryService` / `DiaryService`: Diary CRUD (orders by Author then Title)
- `IDiaryEntryService` / `DiaryEntryService`: Entry CRUD
- `IDiaryArchiveService` / `DiaryArchiveService`: Archive operations (diary + entries bundle)

### Database Models

| Model | Table | Key Fields |
|---|---|---|
| DiaryDTO | Diary | DiaryId (Guid PK), Title (5-50 chars), Author (5-50 chars), Description |
| DiaryEntryDTO | DiaryEntry | DiaryEntryId (Guid PK), Date, Location, Entry, DiaryId (FK) |
| DiaryArchiveDTO | (composite) | Diary + List\<DiaryEntryDTO\> |

### Database Context

- Class: `DiaryDatabaseContext` (EF Core)
- DbSets: `Diaries`, `DiaryEntries`
- Connection: `AZURE_SQL_CONNECTIONSTRING` or `ConnectionStrings:SqlConnection` (with optional `SA_PASSWORD` override)
- All DateTime properties auto-converted to UTC via `UtcValueConverter`

## Development Commands

### Backend API

```powershell
# Build and run API
pushd src/api
dotnet build ccDiary.sln
dotnet run --project ccDiaryApi/ccDiaryApi.csproj
# API available at https://localhost:7183 with Swagger at /swagger

# Run tests
dotnet test ccDiaryApiTest/ccDiaryApiTest.csproj
```

### Frontend UI

```bash
# Install dependencies and run dev server
cd src/ui
npm install
npm run dev
# UI available at http://localhost:8080
```

### All npm Scripts

| Script | Command |
|---|---|
| `dev` | `vite --mode dev` |
| `build` | `vue-tsc --noEmit && vite build` |
| `preview` | `vite preview` |
| `lint` | `eslint . --fix --ignore-path .gitignore` |
| `test` | `vitest` |
| `test:ci` | `vitest run --reporter=default --reporter=junit --outputFile=reports/junit.xml --coverage` |
| `coverage` | `vitest run --coverage` |
| `test:ui` | `vitest --environment jsdom --coverage --ui` |

## Testing

### API Tests (MSTest)

- Framework: MSTest 3.6.3 with Moq 4.20.72
- Coverage: coverlet.collector + coverlet.msbuild
- Test host: `CustomWebApplicationFactory` (SQLite in-memory, TestAuthHandler)
- Location: `src/api/ccDiaryApiTest/`
- Integration tests: DiaryIntegrationTest, DiaryEntryIntegrationTest, DiaryArchiveIntegrationTests
- Controller tests: DiaryControllerTest, DiaryEntryControllerTest, DiaryArchiveControllerTest, WeatherForecastControllerTest

### Frontend Tests (Vitest)

- Framework: Vitest ^2.1.3 with happy-dom (default environment)
- Coverage: v8 provider with cobertura + text + html reporters
- Setup: `tests/setupTests.ts`
- Location: `src/ui/tests/`

## Database Operations

### Migrations

```powershell
# From src/api directory
# Add new migration
dotnet ef migrations add MigrationName --project ccDiaryApi --startup-project ccDiaryApi

# Apply migrations (also automatic on startup via app.MigrateDatabase())
dotnet ef database update --project ccDiaryApi
```

### Current Migrations

1. `InitialCreate` - Creates Diary and DiaryEntry tables
2. `AddTitleAndAuthorToDiar` - Adds Title and Author to Diary
3. `AddDiaryAndMadeLocationAndEntryRequiredToDiaryEntry` - Adds FK, makes Location/Entry required

## Development Workflow

1. **Setup**: Run `scripts/setuplocal.ps1` for initial environment setup (Azure auth, Entra app, certs, .env files, user secrets)
2. **Build**: `dotnet build` for backend, `npm install && npm run build` for frontend
3. **Run**: `dotnet run` for backend, `npm run dev` for frontend
4. **Test**: `dotnet test` for backend, `npm run test` for frontend

## Authentication

### Microsoft Entra ID Setup

- Handled by `entraSetup.ps1` (creates/updates Entra app registration)
- Configures SPA + Web redirect URIs, implicit grant
- Creates OAuth 2.0 scope: `Diary.Update`
- Identifier URI: `api://{appId}`

### API Authentication

- JWT Bearer authentication via Microsoft.Identity.Web
- Configuration section: `Entra` (Instance, ClientId, TenantId, ApplicationIdUri)
- `[Authorize]` attribute on create/update/delete endpoints
- `[AllowAnonymous]` on read endpoints

### Frontend Authentication

- MSAL (@azure/msal-browser) for login/token management
- Config in `services/authentication/msalConfig.ts`
- Service in `services/authentication/msalService.ts`

## Infrastructure as Code (Bicep)

### Deployment

```powershell
# Single environment
./scripts/buildInfrastructure.ps1

# All environments (dev, staging, prod)
./scripts/buildAllInfrastructure.ps1
```

### Azure Resources Created

| Resource | Type | Details |
|---|---|---|
| Resource Group | `rg-{name}-{env}` | Container for all resources |
| Log Analytics | `logs-{appName}` | 30-day retention |
| Container App Environment | Managed | Consumption workload profile |
| Container App | `ca-{appName}` | 0.25 CPU, 0.5Gi, 0-1 replicas, port 8080 |
| SQL Server | `sql-{appName}` | Entra ID admin, public network, Azure services FW rule |
| SQL Database | `sqldb-{appName}` | GP_S_Gen5_1 serverless, 32GB max, free limit + auto-pause |
| Static Web App | `stapp-{appName}` | Free tier |
| Custom Domain | (prod only) | External domain on Static Web App |

### buildInfrastructure.ps1 Post-Deploy Steps

1. Deploys Bicep template
2. Runs `entraSetup.ps1` for cloud environment
3. Configures SQL firewall for current IP
4. Sets Container App env vars (Entra config, ASPNETCORE_ENVIRONMENT)
5. Creates service connector (Container App → SQL Database)
6. Generates Entra client credentials
7. Creates Azure service principal for CI/CD
8. Configures GitHub Actions secrets/variables

## Docker

### Compose Services

| Service | Image | Port | Details |
|---|---|---|---|
| ccdiary (DB) | `mssql/server:2022-latest` | 51433:1433 | SQL Server with health check |
| ccdiaryapi | Built from Dockerfile | 54628:8080 | Depends on healthy DB |

### Container App Image

- Base: `mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim`
- Health check: `curl http://localhost:8080/health` (30s interval, 3 retries)
- Requires pre-published artifacts in `./publish/`

## Important Configuration

### Backend Config Hierarchy

- `appsettings.json` → base config (Entra placeholders, Serilog, Steeltoe actuators)
- `appsettings.{Environment}.json` → environment overrides
- User secrets (Local/LocalContainer environments)
- Environment variables

### Key Environment Variables

- `AZURE_SQL_CONNECTIONSTRING` / `ConnectionStrings:SqlConnection` - Database connection
- `SA_PASSWORD` - Optional password override for connection string
- `DisableHttpsRedirection` - Skip HTTPS redirect (Linux containers)
- `Entra:ClientId`, `Entra:TenantId`, `Entra:ApplicationIdUri` - Auth config

### Frontend Config

- `public/config.js` - Runtime configuration (loaded at startup)
- `.env.dev.local` - Local dev environment variables (created by scripts/setuplocal.ps1)
- `src/utils/appConfig.ts` - `getAppConfigField()` reads from runtime config

## Development Tips

1. API versioning is explicit - routes include `/api/v1/`
2. Frontend auto-import system means components and Vue/Router APIs don't need explicit imports
3. Database changes always require EF Core migrations (auto-applied on startup)
4. Tests are both integration-level and controller-level for API
5. `scripts/setuplocal.ps1` handles all local setup: Azure auth, Entra app, certs, .env, user secrets
6. Docker Compose uses `.env` file in `src/api/` for secrets (generated by scripts/setuplocal.ps1)
7. CORS is wide open (all origins) - suitable for development; review for production
8. Steeltoe actuators available at `/actuator` (health, info, metrics)

---

## Resources

- [ASP.NET Core Documentation](https://learn.microsoft.com/aspnet/core)
- [Vue.js 3 Guide](https://vuejs.org/)
- [Vuetify 3 Documentation](https://vuetifyjs.com/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/ef/core)
- [Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Microsoft Entra ID Docs](https://learn.microsoft.com/entra/identity/)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [MSAL.js Browser](https://learn.microsoft.com/entra/msal/js/)

---

**Last Updated**: 2026-03-22
**Created For**: Claude Code instances working on ccDiary repository
