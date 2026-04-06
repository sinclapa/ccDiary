# ccDiary Repository

This file provides guidance to AI coding assistants (GitHub Copilot, Claude Code, etc.) when working with code in this repository.

## Project Overview

ccDiary is a full-stack diary application that allows users to create, manage, and view diary entries with modern cloud-native architecture

## Technology Stack

### Backend (API)

- Framework: ASP.NET Core 8 (target framework: net8.0)
- Architecture: RESTful API with API versioning
- Database: SQL Server with Entity Framework Core 9.0 (SqlServer + InMemory/Sqlite for tests)
- Authentication: Microsoft Identity Web 4.3.0 (JWT Bearer via Microsoft Entra ID, config section "Entra")

### Frontend (UI)

- Framework: Vue.js
- Styling: Vuetify
- Build Tool: Vite
- Language: TypeScript
- State Management: Pinia
- Authentication: @azure/msal-browser

### Infrastructure & DevOps

- IaC: Bicep (targeting Azure subscription scope)
- Containerization: Docker for API
- Cloud Platform: Microsoft Azure (Container Apps, SQL Database serverless, Static Web Apps, Entra ID)
- Code Quality: SonarCloud (3 separate projects: API, UI, Infra — quality gate blocks CI on failure)

## Repository Structure

```
ccDiary/
├── data/                              # Database initialization & sample data
├── deploy/                            # Infrastructure as Code (Bicep)
├── scripts/                           # Setup and deployment scripts
└── src/                               # Application source code
    ├── api/                           # Backend API (ASP.NET Core)
    │   ├── ccDiaryApi/                # Main API project
    │   │   ├── Controllers/v1/        # API v1 controllers
    │   │   ├── Data/
    │   │   │   ├── Context/           # Database interactions
    │   │   │   └── Model/             # Data models
    │   │   │
    │   │   ├── Services/              # Service layer
    │   │   ├── Extensions/            # Extensions
    │   │   ├── Utilities/             # Utility code
    │   │   ├── Migrations/            # EF Core migrations
    │   │   └── Properties/            # Assembly properties
    │   │
    │   └── ccDiaryApiTest/            # API tests (MSTest framework)
    │       ├── Integration/           # Integration tests
    │       └── v1/                    # API v1 controller tests
    │
    └── ui/                            # Frontend Vue.js application
        ├── public/                    # Runtime config and static files
        └── src/
            ├── components/            # Components 
            ├── pages/                 # File-based routing (unplugin-vue-router)
            ├── services/
            │   ├── authentication/    # MSAL auth
            │   ├── models/            # TypeScript interface
            │   └── modules/           # Services
            │
            ├── stores/                # Pinia store
            ├── layouts/               # Layout wrapper
            ├── plugins/               # Plugin registration (router, pinia, vuetify)
            ├── router/                # Vue Router configuration
            ├── styles/                # Vuetify SCSS settings override
            ├── utils/                 # Utilities 
            ├── assets/                # Images, fonts, static resources
            └── tests/                 # Frontend tests (Vitest + happy-dom)
```

## Key API Endpoints & Services

### API Structure

- Versioning: Asp.Versioning 8.1.0 (URL segment-based: `/api/v{version}/`)
- Route pattern: `api/v{version}/{Controller}/{Action}`
- Current version: v1

### Database Models

| Model | Table | Key Fields |
|---|---|---|
| DiaryDTO | Diary | DiaryId (Guid PK), Title (5-50 chars), Author (5-50 chars), Description |
| DiaryEntryDTO | DiaryEntry | DiaryEntryId (Guid PK), Date, Location, Entry, DiaryId (FK) |
| DiaryArchiveDTO | (composite) | Diary + List\<DiaryEntryDTO\> |

## Development Commands

### Backend API (src/api)
- Build: `dotnet build ccDiary.sln`
- Test and Coverage: `dotnet test ccDiary.sln -c Release --settings ccDiary.runsettings --collect:"XPlat Code Coverage" --results-directory .\TestResults\coverage-api`
- Run: `dotnet run --project ccDiaryApi\ccDiaryApi.csproj`
- Add Migration: `dotnet ef migrations add <Name> -p ccDiaryApi -s ccDiaryApi`
- Update Database: `dotnet ef database update -p ccDiaryApi -s ccDiaryApi`
- Format: `dotnet format .\ccDiary.sln`

### Frontend UI (src/ui)
- Install packages: `npm install`
- Test and Coverage: `npm run test:ci`
- Build and Publish: `npm run build`
- Run: `npm run dev`
- Format: `npm run lint`

### End to end tests (src/ui)
- Ensure Backend API and Frontend UI are running
- Run: `npm run test:e2e`

### Scripts (scripts)
| Script | Description |
|---|---|
| buildAllInfrastructure.ps1 |  |
| buildInfrastructure.ps1 | Build infrastructure in Azure |
| ensure-local-apps-running.ps1 | Run UI and API if not running |
| run-coverage-summary.ps1 | Run coverage for API and UI |
| setuplocal.ps1 | Setup local environment |

## Testing

### API Tests (MSTest)

- Framework: MSTest with Moq
- Coverage: coverlet.collector + coverlet.msbuild
- Location: `src/api/ccDiaryApiTest/`

### Frontend Tests (Vitest)

- Framework: Vitest with happy-dom
- Coverage: v8 provider with cobertura + text + html reporters
- Location: `src/ui/tests/`

### Integration Tests (Playwright)

- Framework: Playwright
- Location: `src/ui/e2e/`

## Development Workflow

1. **Setup** Run `scripts/setuplocal.ps1` for initial environment setup
2. **Format**
3. **Build**
4. **Run**
5. **Test**
6. **Integration Tests**
7. **Check Branch Coverage** is >85%

## Git Workflo
- Branch naming: lowercase and `-` separated using only `a-z`and `0-9`
- Commit format: `type: description` (feat, fix, refactor, test, docs, feat!), breaking changes have type `feat!` or contain `BREAKING CHANGE` 
- Always create a branch before changes
- Run tests before committing

## Infrastructure as Code (Bicep)

### Deployment

```powershell
# Single environment
./scripts/buildInfrastructure.ps1

# All environments (dev, staging, prod)
./scripts/buildAllInfrastructure.ps1
```

## Code Quality (SonarCloud)

The project uses SonarCloud for static analysis across three separate projects. Quality gate failure blocks the CI pipeline (`qualitygate.wait=true`).
Requires >85% overall code coverage and >85% on branch.

### SonarCloud Projects

SonarCloud organization (`cookingcode`)

| Project Key | Scope | Config |
|---|---|---|
| `cookingcode_ccDiary_api` | `src/api/` — C# API code | CLI args in `build-api` CI job |
| `cookingcode_ccDiary_ui` | `src/ui/src/`, `src/ui/tests/` | `sonar-project.properties` (repo root) |
| `cookingcode_ccDiary_infra` | `deploy/`, `scripts/`, `data/`, `*.ps1` | `sonar-project-infra.properties` (repo root) |

---

**Last Updated**: 2026-04-05
