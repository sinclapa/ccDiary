# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

`.github/copilot-instructions.md` is a near-duplicate of this file for GitHub Copilot. When you change shared guidance here, mirror it there.

## Project Overview

ccDiary is a full-stack diary application — ASP.NET Core 8 API + Vue 3/Vuetify SPA, deployed to Azure (Container Apps + Static Web Apps + serverless SQL), authenticated with Microsoft Entra ID.

## Technology Stack

### Backend (API)

- Framework: ASP.NET Core 8 (target framework: net8.0), nullable + implicit usings enabled
- Architecture: RESTful API with URL-segment API versioning, thin controllers over an injected service layer
- Storage: Azure Table + Blob Storage via `Azure.Data.Tables` / `Azure.Storage.Blobs` (Azurite locally and in CI). No ORM and no migrations — see `docs/data-model.md`
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
- Cloud Platform: Microsoft Azure (Container Apps, Table + Blob Storage, Static Web Apps, Entra ID)
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
    │   │   │   ├── Storage/           # TableStore, BlobStore, StorageKeys, TableJson
    │   │   │   └── Model/             # Entities + enums + PagedResultDTO
    │   │   ├── Endpoints/             # Minimal-API endpoints (assembly info)
    │   │   ├── Extensions/            # OTel, request logging, claims, app builder
    │   │   ├── Health/                # Steeltoe IHealthContributor implementations
    │   │   ├── Infrastructure/       # StorageBootstrapper (creates tables/containers at boot)
    │   │   └── Services/              # Business logic behind I*Service interfaces
    │   ├── ccDiaryApiTest/            # MSTest: Integration/ (WebApplicationFactory) + v1/ (unit)
    │   └── ccDiary.Migrate/           # One-shot SQL -> storage migration tool (--verify gates it)
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

| Upstream | Purpose | Cached in |
|---|---|---|
| OSM tile servers | Raster tiles | blob `mapcache/tiles/{source}/{z}/{x}/{y}` |
| Nominatim | Geocoding | table `geocodingcache`, row key `SHA256(normalised query)` |
| OSRM | Routing | blob `mapcache/routes/{profile}/{quantised coords}.json` |

Tiles and routes are blobs rather than table rows for three reasons: they exceed the 64 KB property cap, a blob's own last-modified timestamp serves as the expiry clock, and a **lifecycle policy evicts them automatically after 90 days** — the relational version had no eviction at all and grew without bound.

Route keys quantise coordinates to six decimal places so the lookup is an exact-match fetch. Adding a map data source means a new key shape in `StorageKeys`, not a schema change.

### Storage bootstrap on startup

There are no migrations. `StorageBootstrapper` is an `IHostedService` that creates the six tables and three containers if absent, records the running version in the `appinfo` row, and seeds the first administrator. Creating something that already exists is idempotent, so every replica runs it on every boot and a scale-out cannot race.

Throwing there stops the host, which the deploy workflow already treats as a failed revision — that is what replaces the old pending-migrations gate. `StorageHealthContributor` then point-reads the `appinfo` row, so a missing bootstrap shows up as a DOWN health check rather than a silently empty application.

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

Dates are stored and returned as UTC via `UtcDateTimeJsonConverter` on the storage serializer.

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

# Migration tool (SQL -> storage; --verify is the acceptance gate)
dotnet run --project ccDiary.Migrate -- --source "<sql>" --dest <account> --dry-run
dotnet run --project ccDiary.Migrate -- --source "<sql>" --dest <account> --verify
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
| buildAllInfrastructure.ps1 | Deploy dev → staging → prod sequentially; **stops at the first failure** |
| buildInfrastructure.ps1 | Provision one environment end to end (bicep, Entra, RBAC, GitHub secrets/variables) |
| entraSetup.ps1 | Create or update the Entra app registration; called by the two setup scripts |
| startLocal.ps1 | Ensure Azurite, then run API and UI if not already running |
| stopLocal.ps1 | Kill UI and API processes (preserves VS Code and Visual Studio) |
| run-coverage-summary.ps1 | Ensure Azurite, then run coverage for API and UI |
| setuplocal.ps1 | Setup local environment |

Ordering in `buildAllInfrastructure.ps1` is deliberate: each environment rehearses the next, so a dev failure is evidence prod should not be attempted. Remaining environments are reported as `Skipped`, not omitted.

#### Re-running `buildInfrastructure.ps1` against a live environment

This is the part that is not visible from any single file. The bicep template is **authoritative for the container spec**, but most application configuration is applied *after* deployment, because it depends on outputs that deployment produces (the container and static site FQDNs feed the Entra app registration, which yields the client id and secret). Anything the template does not declare is therefore erased on redeployment.

Three parameters exist solely to feed the running state back in — `existingEnvVars`, `existingSecretRefs`, `existingSecrets` — captured from the deployed app immediately before deploying. Removing that plumbing takes the environment down rather than merely losing configuration: the app fails fast without `Storage__AccountName`, and ingress sends 100% of traffic to the latest revision. A template that omits `secrets` deletes them, leaving every `secretRef` pointing at nothing.

The deployed **image tag** is read back and re-passed for the same reason. `DevApiContainerImage` is an untagged dev reference, and unlike the environment variables the script never sets the image again afterwards, so passing it would permanently and silently roll a promoted environment to `:latest`.

The deployment runs **twice**: the Entra client secret cannot exist until the first run has produced the URLs the app registration is built from.

#### Credentials

- Sensitive values are **container app secrets** referenced with `secretref:`, never inline environment variables. An inline value is part of the container spec, so `az containerapp show`, what-if diffs and any CLI error that echoes its arguments print it in full.
- `az ad app credential reset` returns `appId`/`password`/`tenant` and **no `keyId`**, so the credentials to retire are captured *before* the new one is issued. Identifying the survivor from the reset output yields null and deletes everything.
- An app registration caps at two secrets. `entraSetup.ps1` mints one only when passed `-CreateClientSecret` (`setuplocal.ps1` does, `buildInfrastructure.ps1` does not, since it issues its own), and evicts only secrets it created.

#### Windows shell hazard

`az` is a batch file, so **cmd.exe re-parses the command line after PowerShell has stripped the quotes**. Values containing `|`, `&` or `()` break: a password is split into fragments, and JMESPath `length()` dies with `--output was unexpected at this time`. This is not reliably escapable — passing a value plainly is rejected, and passing it with embedded quotes can return exit 0 while doing nothing at all.

Consequently: secrets travel in the deployment **parameter file** (BOM-less UTF-8 via `WriteAllText`; `Set-Content` emits a BOM that az refuses), and `--query` expressions avoid parentheses — project the field and count in PowerShell instead.

## Testing

### API (MSTest + Moq)

- Unit tests in `ccDiaryApiTest/v1/`, integration tests in `ccDiaryApiTest/Integration/`.
- Integration tests use `CustomWebApplicationFactory` — boots the real `Program` against **Azurite**, with a per-fixture table and container name prefix so parallel classes cannot collide. Mocks `IGraphService`, and exposes `ClearDatabaseAsync()` (call from `[TestInitialize]`; deliberately leaves the `appinfo` row alone, since the bootstrapper writes it once at startup), `CreateAppUserAsync(oid, role)`, `DefaultUserId`, `GraphRedeemUrl`.
- Auth is faked by `TestAuthHandler`; to test a policy, seed an `AppUser` with the right `AppRole` — setting a role claim directly won't reflect the real enrichment path.
- `InternalsVisibleTo("ccDiaryApiTest")` is set, so `internal static` helpers on `Program` (e.g. `ConfigureApiVersioning`) are directly testable — that is how startup config is covered.
- `ccDiary.runsettings` excludes framework modules from coverage. Azurite must be running: `docker compose -f src/api/docker-compose.yml up -d azurite`. The storage tests **fail** rather than skip when it is absent, so CI cannot go green without it.

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

### Storage keys and schema evolution

- **The functions in `StorageKeys` are frozen.** There is no migration step: changing how a key is derived does not move existing rows, it orphans them, because lookups start addressing a location nothing was ever written to. Adding a new key shape is fine; altering an existing one is a data migration.
- A new property appears with its CLR default on rows written before it existed — that is the whole evolution mechanism. **Renaming a property silently loses its data**, so treat a rename as a migration too.
- The storage serializer deliberately disables required-property enforcement. `[JsonRequired]` is right for the HTTP contract but would turn "fall back to a default" into a hard failure when reading an older row.
- Broken-out columns exist only to be filtered on. Derive their values with `ToStoredValue()` rather than writing literals, so a filter cannot drift from the payload.

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

Jobs: `build-prep` (semver bump + tag) → `build-api` (Sonar scan wraps build+test, publish, push image to GHCR) → deploy to Container App (deploy → wait for revision → health check; no wake step and no migration flag, since storage is always on) → `build-ui` (build, test, Sonar, deploy Static Web App with `config.js` substitution).

After pushing a CI/deploy fix, report the run URL rather than polling `gh run list/view`. When a deploy fails, read the actual logs before adding more logging.

## Local Configuration

Sensitive values are never committed — use **user secrets** (`dotnet user-secrets`, id in the csproj) for local `dotnet run`, and **environment variables** for containers.

| Key | Purpose |
|---|---|
| `Storage:AccountName` | Storage account name; the Container App authenticates with its managed identity, so no secret is stored |
| `Storage:ConnectionString` | Used instead of the above for Azurite locally |
| `Entra:ClientId` / `Entra:TenantId` / `Entra:ApplicationIdUri` | Entra ID app registration |
| `DisableHttpsRedirection` | Set when running behind a proxy (Codespaces, Container Apps) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP collector base URL (optional — OTel disabled when absent) |
| `OTEL_EXPORTER_OTLP_HEADERS` | Comma-separated `key=value` auth headers for OTLP |

Environment names are matched case-insensitively in `Program.cs` for `Local`, `LocalContainer`, and `LocalCompose` — user secrets load for all of them.

In the deployed environments `Graph__ClientSecret`, `Smtp__Password` and `OTEL_EXPORTER_OTLP_HEADERS` are **container app secrets**, so the container spec shows `secretRef` names rather than values. Set them through the deployment, not `az containerapp update --set-env-vars`.

**Azurite must be running for anything that touches storage** — it is the whole persistence tier, so the API's `StorageBootstrapper` throws and the host never starts without it. The symptom is a port timeout, not a storage error. `startLocal.ps1` and `run-coverage-summary.ps1` start it; otherwise `docker compose -p ccdiary -f src/api/docker-compose.yml up -d azurite`. Compose owns the single definition — do not `docker run` a second container, since it claims the same name.

### OpenTelemetry (API)

Configured in `OpenTelemetryExtensions.cs`. When `OTEL_EXPORTER_OTLP_ENDPOINT` is set:

- **Traces** — ASP.NET Core, HttpClient, and the Azure SDKs via `AddSource("Azure.*")` → `{endpoint}/v1/traces`
- **Metrics** — ASP.NET Core, HttpClient, runtime → `{endpoint}/v1/metrics`
- **Logs** — Serilog OTLP sink → `{endpoint}/v1/logs`

Signal paths are appended explicitly because the SDK disables auto-append when the endpoint is set programmatically. The exporter uses HTTP/Protobuf with a **batch processor** on a 2 second schedule, and `ApplicationStopping` force-flushes tracer/meter providers on SIGTERM so a scale-to-zero shutdown does not drop in-flight spans.

Tracing excludes `/swagger`, `/actuator`, `/api/assembly-info` and `/health`.

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

**Last Updated**: 2026-08-18
