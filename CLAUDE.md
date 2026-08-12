# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

`.github/copilot-instructions.md` is a near-duplicate of this file for GitHub Copilot. When you change shared guidance here, mirror it there.

## Project Overview

ccDiary is a full-stack diary application — ASP.NET Core 8 API + Vue 3/Vuetify SPA, deployed to Azure (Container Apps + Static Web Apps + serverless SQL), authenticated with Microsoft Entra ID.

## Technology Stack

### Backend (API)

- Framework: ASP.NET Core 8 (target framework: net8.0), nullable + implicit usings enabled
- Architecture: RESTful API with URL-segment API versioning, thin controllers over an injected service layer
- Database: SQL Server with Entity Framework Core 9.0 (SqlServer in app; Sqlite in-memory for integration tests)
- Authentication: Microsoft Identity Web 4.3.0 (JWT Bearer via Microsoft Entra ID, config section "Entra")
- Observability: Serilog + OpenTelemetry (OTLP) + Steeltoe actuators

### Frontend (UI)

- Framework: Vue 3 (`<script setup>`, Composition API)
- Styling: Vuetify 3
- Build Tool: Vite 5
- Language: TypeScript
- State Management: Pinia
- Authentication: @azure/msal-browser
- Maps: Leaflet
- Observability: Grafana Faro (RUM)

### Infrastructure & DevOps

- IaC: Bicep (targeting Azure subscription scope)
- Containerization: Docker for API, image pushed to GHCR
- Cloud Platform: Microsoft Azure (Container Apps, SQL Database serverless, Static Web Apps, Entra ID)
- Code Quality: SonarCloud (3 separate projects: API, UI, Infra — quality gate blocks CI on failure)

## Repository Structure

```
ccDiary/
├── data/                              # Database initialization & sample data
├── deploy/                            # Bicep: main / resourceGroup / containerApps
├── scripts/                           # Setup and deployment scripts
└── src/
    ├── api/                           # ccDiary.sln
    │   ├── ccDiaryApi/
    │   │   ├── Authorization/         # AppUserEnrichmentMiddleware (DB role → claim)
    │   │   ├── Controllers/v1/        # API v1 controllers
    │   │   ├── Data/
    │   │   │   ├── Context/           # DiaryDatabaseContext, UtcValueConverter
    │   │   │   ├── Migration/         # DiaryDatabaseMigrationManager
    │   │   │   └── Model/             # Entities + enums + PagedResultDTO
    │   │   ├── Endpoints/             # Minimal-API endpoints (assembly info)
    │   │   ├── Extensions/            # OTel, request logging, claims, app builder
    │   │   ├── Health/                # Steeltoe IHealthContributor implementations
    │   │   ├── Migrations/            # EF Core migrations + model snapshot
    │   │   └── Services/              # Business logic behind I*Service interfaces
    │   └── ccDiaryApiTest/            # MSTest: Integration/ (WebApplicationFactory) + v1/ (unit)
    └── ui/
        ├── e2e/                       # Playwright specs
        ├── public/                    # config.js runtime config + static files
        ├── tests/                     # Vitest specs mirroring src/ layout
        └── src/
            ├── components/            # Auto-registered globally (no imports needed)
            ├── composables/           # Reusable logic + co-located __tests__/
            ├── pages/                 # File-based routing (unplugin-vue-router)
            ├── services/{authentication,models,modules}/
            ├── stores/                # Pinia stores (auth, app, apiStatus)
            ├── layouts/ plugins/ router/ styles/ utils/ assets/
```

Note: UI unit tests live in **two** places — `src/ui/tests/**` (components, pages, services, stores) and co-located `__tests__/` folders under `src/composables`, `src/utils`, `src/plugins`. Follow whichever convention the neighbouring code already uses.

## Architecture — things you need multiple files to see

### Authorization: roles come from the database, not the JWT

This is the single most important cross-cutting flow. Entra tokens carry **no** app roles. Instead:

1. `UseAuthentication()` validates the JWT and populates `ClaimsPrincipal`.
2. `AppUserEnrichmentMiddleware` (`UseAppUserEnrichment()`) reads the `oid` claim (`ClaimsPrincipalExtensions.GetOid()`), looks up the `AppUser` row via `IUserService`, and **adds a `ClaimTypes.Role` claim** from `AppUser.Role`.
3. `UseAuthorization()` then evaluates the policies registered in `Program.cs`:
   - `DiaryAdmin` → requires role `DiaryAdmin`
   - `DiaryContributor` → requires `DiaryAdmin` **or** `DiaryContributor`

The middleware order in `Program.cs` is load-bearing: `UseAuthentication` → `UseObservabilityUserContext` → `UseAppUserEnrichment` → `UseAuthorization`. Moving enrichment after `UseAuthorization` silently breaks every policy-protected endpoint.

A user with no `AppUser` row is authenticated but role-less — they can read but not write, and the UI routes them to the access-request/register flow (`AccessRequestController` → `EmailService`/`GraphService` invite).

`UserService.SeedBootstrapAdminAsync()` runs once at startup to create the first admin. Note: personal Microsoft accounts get a different OID in issued JWTs than `az ad signed-in-user show` reports — decode a real token to get the seed OID.

### Enum serialization is kebab-case across the wire

`Program.cs` registers `JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower)`. So `AppRole.DiaryAdmin` serialises as `"diary-admin"`, which is exactly what `src/ui/src/stores/auth.ts` compares against. Renaming a C# enum member is a breaking API change — grep the UI for the kebab-case string.

### UI runtime configuration (build once, deploy anywhere)

The UI is built once and configured at deploy time, not build time:

- `public/config.js` sets `globalThis.APP_CONFIG` with every key as the literal `'__PLACEHOLDER__'`.
- CI rewrites `dist/config.js` per environment during the Static Web App deploy.
- `utils/appConfig.ts` → `getAppConfigField(name)` resolves in order: `APP_CONFIG[name]` (if not the placeholder) → `import.meta.env[name]` → the sentinel `'NOT_SET'`.

Consequences: **optional features are gated on `!== 'NOT_SET'`** (e.g. Faro in `plugins/faro.ts`), and any new runtime setting must be added to `public/config.js` *and* the CI substitution step, not just to `.env`.

Locally, Vite modes supply the values: `.env.dev` (`npm run dev`, API on `https://localhost:7183`) and `.env.devcompose` (`npm run devcompose`, API on `:7184`). Secrets live in the gitignored `.env.*.local` files.

### Map / journey feature is proxied and cached server-side

Diary entries can pin a location (`MapLocation`, `ShowMap`) or draw a journey (`FromLocation`/`ToLocation`, `ShowJourney`, `JourneyMode`). The browser never calls OSM services directly — `MapTileController` proxies them so the app can set a compliant User-Agent (the named `"MapTileProxy"` HttpClient) and cache responses in SQL:

| Upstream | Purpose | Cache table |
|---|---|---|
| OSM tile servers | Raster tiles | `MapTileCache` |
| Nominatim | Geocoding | `GeocodingCache` |
| OSRM | Routing | `RoutingCache` |

Each cache is time-bounded by a `CachedAt` cutoff. Adding a map data source means adding a cache DTO + migration, not just an HTTP call.

### Database migration on startup

`Program.cs` runs migrations at boot only when `RUN_MIGRATIONS` is true (default `true`). The deploy workflow sets it for the migrating revision and then **disables it for subsequent restarts**, so a Container App scale-out doesn't re-race migrations. The SQL database is serverless and may be paused — CI explicitly wakes it before deploying.

### UI auto-imports and generated files

`vite.config.mts` wires up auto-imports; do not add manual imports for these, and **never hand-edit the generated `.d.ts` files** (`src/components.d.ts`, `src/auto-imports.d.ts`, `src/typed-router.d.ts` — they are regenerated on build/dev):

- Every `.vue` file in `src/components` is a global component
- Vue APIs (`ref`, `computed`, …) and `useRoute`/`useRouter` are globally available
- Vuetify components are auto-imported
- `@/` aliases to `src/`

### Database Models

| Model | Table | Notes |
|---|---|---|
| `DiaryDTO` | Diary | DiaryId (Guid PK), Title (5–50), Author (≤50), Description, **OwnerId** |
| `DiaryEntryDTO` | DiaryEntry | Date, Location, Entry, map fields, journey fields, `ImageData`/`ImageContentType` (base64 inline), DiaryId FK |
| `AppUserDto` | AppUser | EntraObjectId (the `oid`), DisplayName, Email, `AppRole` |
| `AccessRequestDto` | AccessRequest | Registration requests + `RequestStatus`, invite redeem URL |
| `MapTileCacheDto` / `GeocodingCacheDto` / `RoutingCacheDto` | *Cache | Server-side caches for the map proxy |
| `AppInfoDTO` | AppInfo | `DatabaseLastUpdated`, surfaced by `AppInfoController` |
| `DiaryArchiveDTO` | (composite) | Diary + `List<DiaryEntryDTO>` |
| `PagedResultDTO<T>` | (transport) | `Items`, `TotalCount`, `Page`, `PageSize` |

Dates are stored/returned as UTC via `UtcValueConverter` in `DiaryDatabaseContext`.

### API surface

- Route pattern: `api/v{version:apiVersion}/[controller]/[action]`, current version `1.0`
- Controllers: Diary, DiaryEntry, DiaryArchive, Admin (`DiaryAdmin` policy), AccessRequest, User, MapTile, AppInfo
- Read actions require authentication; writes require the `DiaryContributor` policy
- Swagger at `/swagger`, Steeltoe actuators at `/actuator`, assembly info at `/api/assembly-info`

## Development Commands

### Backend API (`src/api`)

```powershell
dotnet build ccDiary.sln
dotnet run --project ccDiaryApi\ccDiaryApi.csproj
dotnet format .\ccDiary.sln
dotnet test ccDiary.sln -c Release --settings ccDiary.runsettings --collect:"XPlat Code Coverage" --results-directory .\TestResults\coverage-api

# Single test class / single test
dotnet test ccDiary.sln --filter "FullyQualifiedName~DiaryControllerTest"
dotnet test ccDiary.sln --filter "FullyQualifiedName~DiaryControllerTest.GetDiaries_ReturnsOk"

# EF Core
dotnet ef migrations add <Name> -p ccDiaryApi -s ccDiaryApi
dotnet ef database update -p ccDiaryApi -s ccDiaryApi
```

`RestorePackagesWithLockFile` is on — adding or bumping a NuGet package must update `packages.lock.json` (run a normal `dotnet restore`), or CI's locked restore fails.

### Frontend UI (`src/ui`)

```powershell
npm install
npm run dev            # Vite on port 8080 (strictPort), mode "dev"
npm run devcompose     # against the docker-compose API
npm run build          # vue-tsc --noEmit && vite build — type errors fail the build
npm run lint           # eslint --fix
npm run test:ci        # vitest run + junit + coverage

# Single test file / single test name
npx vitest run tests/components/DiaryTimeline.spec.ts
npx vitest run -t "renders the timeline"
npx vitest tests/stores/auth.spec.ts    # watch mode
```

### End-to-end tests (`src/ui`)

Playwright auto-starts `npm run dev` unless `PLAYWRIGHT_BASE_URL` is set; the API must be running separately.

```powershell
npm run test:e2e
npx playwright test e2e/home.spec.ts
npx playwright test --grep "cookie preferences"
```

### Scripts (`scripts`)

| Script | Description |
|---|---|
| buildAllInfrastructure.ps1 | Deploy infrastructure for all environments (dev, staging, prod) sequentially |
| buildInfrastructure.ps1 | Build infrastructure in Azure |
| startLocal.ps1 | Run UI and API if not running |
| stopLocal.ps1 | Kill UI and API processes (preserves VS Code and Visual Studio) |
| run-coverage-summary.ps1 | Run coverage for API and UI |
| setuplocal.ps1 | Setup local environment |

## Testing

### API (MSTest + Moq)

- Unit tests in `ccDiaryApiTest/v1/`, integration tests in `ccDiaryApiTest/Integration/`.
- Integration tests use `CustomWebApplicationFactory` — boots the real `Program` against a **Sqlite in-memory** connection held open for the fixture's lifetime, mocks `IGraphService`, and exposes helpers: `ClearDatabaseAsync()` (call from `[TestInitialize]`), `CreateAppUserAsync(oid, role)`, `DefaultUserId`, `GraphRedeemUrl`.
- Auth is faked by `TestAuthHandler`; to test a policy, seed an `AppUser` with the right `AppRole` — setting a role claim directly won't reflect the real enrichment path.
- `InternalsVisibleTo("ccDiaryApiTest")` is set, so `internal static` helpers on `Program` (e.g. `ConfigureApiVersioning`) are directly testable — that is how startup config is covered.
- `ccDiary.runsettings` excludes `**/Migrations/*.cs` and framework modules from coverage.

### Frontend (Vitest + happy-dom)

- `tests/setupTests.ts` is the global setup; `tests/plugins/vuetify-test-plugin.ts` supplies the Vuetify instance components need when mounted.
- `vuetify` and `leaflet` are inlined via `test.server.deps.inline` — needed for them to work under happy-dom.
- Coverage: v8 provider, reporters `lcov + cobertura + text + html`. Plugin bootstrap, router, generated `.d.ts`, and `main.ts` are excluded in `vite.config.mts`; don't write tests purely to cover those.

### E2E (Playwright)

- Chromium only, `fullyParallel: false`, 1 retry, screenshots/video retained on failure.
- Specs are locator-sensitive — a Vuetify component swap (e.g. `v-btn` refactor) usually requires updating e2e locators in the same commit.

## Code Conventions

### C#

- StyleCop Analyzers are on (`AnalysisMode: Recommended`, company `CookingCode`). Every file starts with the copyright header and puts `using` directives **inside** the namespace:
  ```csharp
  // <copyright file="Foo.cs" company="CookingCode">
  // Copyright (c) CookingCode. All rights reserved.
  // </copyright>

  namespace ccDiaryApi.Services
  {
      using ccDiaryApi.Data.Model;
      ...
  ```
- Services are interface-first (`IFooService` + `FooService`) and registered `AddScoped` in `Program.cs`.
- Suppressed warnings live in the csproj `NoWarn` (1591, CA1848, NU1608, SA1516, AD0001) — prefer fixing over adding to it.
- `[ExcludeFromCodeCoverage]` requires a written `Justification`.

### Vue / TypeScript

- Always use tuple-style `defineEmits<{ change: [value: string] }>()`, not the call-signature overload form — the overload form trips the `func-call-spacing` lint rule.
- API access goes through `services/modules/*Service.ts`; components should not call `fetch` directly.
- Shared logic belongs in `src/composables` with a co-located `__tests__/` spec.

### EF Core migrations

- When creating a migration manually, also create the matching `.Designer.cs` and make sure the model snapshot carries **every** attribute (MaxLength, Required, …) from the entity.
- Apply it to the local DB and verify the API starts before committing.

## Development Workflow

1. **Setup** — `scripts/setuplocal.ps1`
2. **Format** — `dotnet format .\ccDiary.sln` (API) / `npm run lint` (UI)
3. **Build** — `dotnet build ccDiary.sln` / `npm run build`
4. **Run** — `dotnet run --project ccDiaryApi\ccDiaryApi.csproj` / `npm run dev`
5. **Test** — `dotnet test ...` / `npm run test:ci`
6. **E2E** — with both running, `npm run test:e2e`
7. **Coverage** — must be >85% overall and >85% on branches (SonarCloud gate)

## Git Workflow

- Branch naming: lowercase, `-` separated, `a-z0-9` only
- Commit format: `type: description` (feat, fix, refactor, test, docs); breaking changes use `feat!` or a `BREAKING CHANGE` footer — CI derives the semver bump and git tag from this
- Always branch before changing anything; run tests before committing

## CI/CD (`.github/workflows/build-and-test.yml`)

Jobs: `build-prep` (semver bump + tag) → `build-api` (Sonar scan wraps build+test, publish, push image to GHCR) → deploy to Container App (wake SQL → deploy with `RUN_MIGRATIONS` → wait for revision → health check → disable migrations) → `build-ui` (build, test, Sonar, deploy Static Web App with `config.js` substitution).

After pushing a CI/deploy fix, report the run URL rather than polling `gh run list/view`. When a deploy fails, read the actual logs before adding more logging.

## Local Configuration

Sensitive values are never committed — use **user secrets** (`dotnet user-secrets`, id in the csproj) for local `dotnet run`, and **environment variables** for containers.

| Key | Purpose |
|---|---|
| `ConnectionStrings:SqlConnection` (or `AZURE_SQL_CONNECTIONSTRING`) | SQL Server connection string (template in `appsettings.Local.json`) |
| `Entra:ClientId` / `Entra:TenantId` / `Entra:ApplicationIdUri` | Entra ID app registration |
| `RUN_MIGRATIONS` | Run EF migrations at startup (default `true`) |
| `DisableHttpsRedirection` | Set when running behind a proxy (Codespaces, Container Apps) |
| `SA_PASSWORD` | Overrides the connection string password (docker-compose) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP collector base URL (optional — OTel disabled when absent) |
| `OTEL_EXPORTER_OTLP_HEADERS` | Comma-separated `key=value` auth headers for OTLP |

Environment names are matched case-insensitively in `Program.cs` for `Local`, `LocalContainer`, and `LocalCompose` — user secrets load for all of them.

### OpenTelemetry (API)

Configured in `OpenTelemetryExtensions.cs`. When `OTEL_EXPORTER_OTLP_ENDPOINT` is set:

- **Traces** — ASP.NET Core, HttpClient, EF Core, SqlClient → `{endpoint}/v1/traces`
- **Metrics** — ASP.NET Core, HttpClient, runtime → `{endpoint}/v1/metrics`
- **Logs** — Serilog OTLP sink → `{endpoint}/v1/logs`

Signal paths are appended explicitly because the SDK disables auto-append when the endpoint is set programmatically. The exporter uses HTTP/Protobuf with a **simple processor** (not batch) for scale-to-zero environments, and `ApplicationStopping` force-flushes tracer/meter providers on SIGTERM.

Tracing excludes `/swagger`, `/actuator`, `/api/assembly-info`, `/health`, and filters low-value SQL probes (e.g. `SELECT 1`).

### Grafana Cloud — API (Loki)

| Property | Value |
|---|---|
| Datasource name | `grafanacloud-cookingcode-logs` |
| Datasource UID | `grafanacloud-logs` |
| Service label | `service_name="ccDiaryApi"` |

```logql
{service_name="ccDiaryApi"}
{service_name="ccDiaryApi"} | detected_level="error"
{service_name="ccDiaryApi", deployment_environment="prod"}
```

### Grafana Cloud — UI (Faro)

Configured in `src/ui/src/plugins/faro.ts`; enabled when `VITE_FARO_URL` resolves to something other than `NOT_SET`.

| Property | Value |
|---|---|
| App name | `ccdiary-ui` (primary filter in Grafana) |
| Config key | `VITE_FARO_URL` |
| Environment | `VITE_ENVIRONMENT` |

## Infrastructure as Code (Bicep)

`deploy/main.bicep` (subscription scope) → `resourceGroup.bicep` → `containerApps.bicep`.

```powershell
./scripts/buildInfrastructure.ps1        # single environment
./scripts/buildAllInfrastructure.ps1     # dev, staging, prod
```

## Code Quality (SonarCloud)

Organization `cookingcode`. Quality gate failure blocks CI (`qualitygate.wait=true`). Requires >85% coverage overall and on new code branches.

| Component | Project Key | Scope | Config |
|---|---|---|---|
| **API** | `cookingcode_ccDiary_api` | `src/api/` | CLI args in the `build-api` CI job |
| **UI** | `cookingcode_ccDiary_ui` | `src/ui/src/`, `src/ui/tests/` | `sonar-project.properties` |
| **Infra** | `cookingcode_ccDiary_infra` | `deploy/`, `scripts/`, `data/`, `*.ps1` | `sonar-project-infra.properties` |

Pick the project key matching the directory you are working in. After fixing issues, don't re-query `search_sonar_issues_in_projects` to verify — the server won't reflect the change yet.

---

**Last Updated**: 2026-08-12
