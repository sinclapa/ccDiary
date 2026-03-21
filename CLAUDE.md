# Claude Code Context for ccDiary Repository

  This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

  Project Overview

  ccDiary is a full-stack diary application that allows users to create, manage, and view diary entries with modern
  cloud-native architecture.

  Key Characteristics

  - Type: Full-stack web application
  - Tier: SaaS with authentication and cloud deployment
  - Purpose: Diary entry management with Microsoft Entra ID OAuth integration
  - Maturity Level: Active development (infrastructure automation in progress)
  - Deployment Target: Azure Container Apps via Bicep IaC

  Technology Stack

  Backend

  - Framework: ASP.NET Core 8 (target framework: net8.0)
  - Architecture: RESTful API with API versioning (Asp.Versioning)
  - Database: SQL Server with Entity Framework Core 9.0
  - Authentication: Microsoft Identity Web (OAuth via Microsoft Entra ID)
  - Logging: Serilog with structured logging
  - API Docs: Swagger/OpenAPI with custom authorization filter
  - Monitoring: Steeltoe Management endpoints (Health, Info, Metrics)
  - Code Quality: StyleCop.Analyzers, .NET Analyzers (Recommended mode)
  - Package Lock: Uses packages.lock.json (RestorePackagesWithLockFile enabled)

  Frontend

  - Framework: Vue.js 3 (Composition API)
  - Styling: Vuetify 3 (Material Design)
  - Build Tool: Vite
  - Language: TypeScript
  - State Management: Pinia
  - Router: unplugin-vue-router (type-safe route generation)
  - Authentication: @azure/msal-browser (Microsoft authentication)
  - Component Auto-Import: unplugin-vue-components + unplugin-auto-imports
  - Testing: Vitest with coverage (Istanbul/v8)
  - Linting: ESLint with TypeScript support

  Infrastructure & DevOps

  - IaC: Bicep (targeting Azure subscription scope)
  - Containerization: Docker & Docker Compose
  - Container Registry: Uses image references for Container Apps
  - CI/CD Launch Points: PowerShell scripts (setuplocal.ps1, buildInfrastructure.ps1, etc.)
  - Cloud Platform: Microsoft Azure (Container Apps, SQL Database, Entra ID)

  Development Tools (Recommended)

  - Visual Studio Community (for .NET development)
  - Visual Studio Code (for TypeScript/Vue, infrastructure as code)
  - SQL Server Management Studio (SSMS)
  - Azure CLI
  - Node.js (for UI development)
  - Docker Desktop
  - GitHub CLI
  - Git Bash

  Repository Structure

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

  Key API Endpoints & Services

  API Structure

  - Versioning: Using Asp.Versioning (modern ASP.NET Core API versioning)
  - Controllers: Located in ccDiaryApi/Controllers/v1/
  - Route pattern: /api/v{version}/endpoint
  - Current version: v1

  Main Services

  - DiaryService: Core diary operations
  - DiaryEntryService: Individual diary entry management
  - DiaryArchiveService: Diary archive functionality

  Database Models

  - DiaryEntryDTO: Main diary entry model
  - DiaryDTO: Diary summary data transfer object
  - DiaryArchiveDTO: Archive-related data structure

  Key Development Tasks

  Running the Application

  Backend API

  # Build and run API
  pushd src/api
  dotnet build ccDiary.sln
  dotnet run --project ccDiaryApi/ccDiaryApi.csproj
  # API available at https://localhost:7183 with Swagger UI at https://localhost:7183/swagger

  # Run tests
  dotnet test ccDiaryApiTest/ccDiaryApiTest.csproj

  Frontend UI

  # Install dependencies and run dev server
  cd src/ui
  npm install
  npm run dev
  # UI available at http://localhost:8080

  Testing

  API Tests

  - Framework: xUnit or similar (via ccDiaryApiTest.csproj)
  - Location: src/api/ccDiaryApiTest/
  - Custom factory: CustomWebApplicationFactory.cs
  - Mock auth: TestAuthHandler.cs + TestAuthHandlerOptions.cs

  Frontend Tests

  - Framework: Vitest with Happy DOM / jsdom
  - Location: src/ui/tests/
  - Coverage: Istanbul or v8

  Database Operations

  Migrations

  # Add new migration
  dotnet ef migrations add MigrationName --project ccDiaryApi --startup-project ccDiaryApi

  # Apply migrations (automatic on startup)
  dotnet ef database update --project ccDiaryApi

  Development Workflow

  1. Setup: Run setuplocal.ps1 for initial environment setup
  2. Build: Use dotnet build for backend, npm install + npm run build for frontend
  3. Run:
    - Backend: dotnet run
    - Frontend: npm run dev
  4. Test:
    - Backend: dotnet test
    - Frontend: npm run test

  Authentication

  Microsoft Entra ID Setup

  - Handled by entraSetup.ps1
  - Requires Azure CLI + admin consent
  - Registers application in Azure AD
  - Configures OAuth redirect URIs

  API Authentication

  - Backend requires Microsoft.Identity.Web JWT bearer token validation
  - Validate with [Authorize] attribute
  - Access token claims via User.FindFirst(ClaimTypes.NameIdentifier)

  Frontend Authentication

  - Use MSAL (@azure/msal-browser) for login/token management
  - Call API with access token in Authorization header: Bearer {token}
  - Automatic token refresh via MSAL

  Infrastructure as Code (Bicep)

  Deployment

  ./buildInfrastructure.ps1 -name "ccdiary" -environment "dev"

  Bicep Files Structure

  - main.bicep: Subscription-level entrypoint
  - resourceGroup.bicep: Resources within the resource group
  - containerApps.bicep: Azure Container Apps specific configuration

  Common Development Tasks

  1. Add new API endpoint: Create controller in src/api/ccDiaryApi/Controllers/v1/
  2. Add database entity: Create model class, add DbSet to DbContext, create migration
  3. Add frontend component: Create .vue file in src/ui/src/components/ or src/pages/
  4. Update migrations: Use EF Core commands like dotnet ef migrations add

  Important Configuration Files

  Backend

  - Program.cs: Application startup and DI configuration
  - appsettings.json: Default configuration
  - launchSettings.json: Debugging ports and profiles

  Frontend

  - vite.config.mts: Vite build configuration
  - src/main.ts: Application entry point
  - package.json: npm dependencies and scripts

  Development Tips

  1. API versioning is explicit - routes include /api/v1/
  2. Frontend auto-import system means components and utilities don't need explicit imports
  3. Database changes always require migrations (never manual SQL in production)
  4. Tests are integration-level for API - keep them meaningful
  5. Docker Compose is incomplete - work around it for now

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

**Last Updated**: 2026-03-21
**Created For**: Claude Code instances working on ccDiary repository
