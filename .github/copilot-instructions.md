# ccDiary Repository

This file provides guidance to AI coding assistants (GitHub Copilot, Claude Code, etc.) when working with code in this repository.

## Project Overview

ccDiary is a full-stack diary application that allows users to create, manage, and view diary entries with modern cloud-native architecture

## Technology Stack

### Backend (API)

- Framework: ASP.NET Core 8 (target framework: net8.0)
- Architecture: RESTful API with API versioning
- Storage: Azure Table + Blob Storage via Azure.Data.Tables / Azure.Storage.Blobs (Azurite locally and in CI). No ORM, no migrations
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
- Containerization: Docker for local development only; nothing in Azure runs a container image
- Cloud Platform: Microsoft Azure (Functions Flex Consumption hosts the API, Table + Blob Storage, Static Web Apps, Key Vault, Entra ID)
- Code Quality: SonarCloud (3 separate projects: API, UI, Infra — quality gate blocks CI on failure)

## Repository Structure

```
ccDiary/
├── data/                              # Database initialization & sample data
├── deploy/                            # Bicep: main / resourceGroup / functionApp / *RoleAssignments
├── scripts/                           # Setup and deployment scripts
└── src/                               # Application source code
    ├── api/                           # Backend API (ASP.NET Core)
    │   ├── ccDiaryApi/                # Main API project
    │   │   ├── Controllers/v1/        # API v1 controllers
    │   │   ├── Data/
    │   │   │   ├── Storage/           # TableStore, BlobStore, StorageKeys, TableJson
    │   │   │   └── Model/             # Data models
    │   │   │
    │   │   ├── Services/              # Service layer
    │   │   ├── Extensions/            # Extensions
    │   │   ├── Utilities/             # Utility code
    │   │   ├── Infrastructure/       # StorageBootstrapper (creates tables/containers at boot)
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
- Migrate SQL data: `dotnet run --project ccDiary.Migrate -- --source "<sql>" --dest <account> --verify`
- Rebuild from archive: `dotnet run --project ccDiary.Migrate -- --from-archive data/ww1-diary.json --dest <account>`
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
| buildAllInfrastructure.ps1 | Deploy dev → staging → prod sequentially; **stops at the first failure** |
| buildInfrastructure.ps1 | Provision one environment end to end (bicep, Entra, RBAC, GitHub secrets/variables) |
| entraSetup.ps1 | Create or update the Entra app registration; called by the two setup scripts |
| startLocal.ps1 | Ensure Azurite, then run API and UI if not already running |
| stopLocal.ps1 | Kill UI and API processes (preserves VS Code and Visual Studio) |
| run-coverage-summary.ps1 | Ensure Azurite, then run coverage for API and UI |
| setuplocal.ps1 | Setup local environment |

#### Re-running buildInfrastructure.ps1 against a live environment

The bicep template is authoritative for the container spec, but most application configuration is applied *after* deployment because it depends on outputs that deployment produces. Anything the template does not declare is therefore erased. `existingEnvVars`, `existingSecretRefs` and `existingSecrets` exist solely to feed the running state back in, and the deployed image tag is read back and re-passed — without them a redeploy takes the environment down and rolls it to `:latest`. The deployment runs twice, because the Entra client secret cannot exist until the first run produces the URLs the app registration is built from. The startup and readiness probes poll `/health/startup`, an in-memory flag `StorageBootstrapper` sets when it finishes, served from a branch mounted ahead of the rest of the pipeline. Readiness is declared because the implicit one polls only every 5 s. Its path is declared in the template, so deploy an image that serves it before running the script; otherwise every probe returns 404 and the replica never becomes ready.

`az ad app credential reset` returns no `keyId`, so credentials to retire are captured before the new one is issued. An app registration caps at two secrets; `entraSetup.ps1` mints one only with `-CreateClientSecret` and evicts only its own.

The function app's settings work the opposite way: ARM replaces `siteConfig.appSettings` wholesale, so the template is authoritative for all of them, the script passes the complete set, and the deploy workflows set none — a name missing from the script is removed from the app. Its three credentials live in Key Vault and the settings carry `@Microsoft.KeyVault(SecretUri=...)` references the platform resolves with the app's managed identity, since an inline value would be readable through `az functionapp config appsettings list`. The CI service principal also needs Storage Blob Data Contributor: Contributor manages the storage account but cannot write the deployment package blob, and shared-key access is disabled. That principal holds no password: the workflows sign in with OIDC against a federated credential on `ca-ccdiary-<env>-credentials` whose subject is `repo:sinclapa/ccDiary:environment:<env>`, so the signing-in jobs need `id-token: write` and there is no `AZURE_CREDENTIALS` secret. Since the subject names the environment, each environment's deployment branch policy decides which refs can reach that Azure access.

#### Hosting: the API runs as a Functions custom handler

The API is an ordinary ASP.NET Core app; nothing in `Program.cs` knows it is hosted by Azure Functions. Three files at the root of `ccDiaryApi` make it one: `host.json` (custom handler with `enableProxyingHttpRequest` and `routePrefix: ""`), `proxy/function.json` (one anonymous catch-all HTTP trigger, `route: "{*path}"`), and `run.sh` (execs the binary with `--urls http://127.0.0.1:$FUNCTIONS_CUSTOMHANDLER_PORT`).

Two consequences are invisible from the app's code. CORS must be configured on the platform (`siteConfig.cors.allowedOrigins`), because the Functions front end answers preflight `OPTIONS` itself and never forwards it, so `app.UseCors` never sees it and browsers block authenticated calls. And the package must be zipped on Linux, because `ccDiaryApi` and `run.sh` need their executable bit, which a Windows-built zip drops.

Deploying is not `az functionapp deploy` — that endpoint rejects this package (415 for a trivial zip, 502 for the real one). The workflows upload it to the deployment container as `released-package.zip`, call `syncfunctiontriggers`, then restart the app. Both deploy paths share `.github/actions/deploy-function-app` and `.github/actions/configure-ui-runtime-config`, so change the composite action rather than one workflow. The API deploys and passes its health check *before* the UI in every environment, so a function app that never comes up leaves that environment on the previous version rather than stranding a new UI in front of it; `release-prod.yml` also takes a `workflow_dispatch` with a `tag` to redeploy an earlier release, which is the rollback path. In `build-and-test.yml` the API deploy, the UI deploy and the end-to-end run are one job (`deploy-and-verify`) holding one concurrency group, because every PR deploys to the single dev function app and a group spanning separate jobs would let the next run deploy between them. Renaming that job means updating the required status checks on `main`.
#### Rolling an environment onto Flex Consumption

Per environment: run `buildInfrastructure.ps1 -EnvironmentParam <env>`, then deploy the package (upload `released-package.zip`, `syncfunctiontriggers`, restart), then let CI redeploy that environment's UI — `API_URL` is baked into `config.js` at deploy time, so the site keeps calling the Container App until then. That last step is the actual switch, and redeploying the UI against the Container App URL is the rollback. For prod, run the infrastructure and deploy the package *before* publishing the release: the Functions steps are guarded on `FUNCTION_APP_NAME`, so publishing first skips them.


The reason for this hosting is cold start: ~23 s median on Container Apps, 13–22 s of it Azure scheduling a pod, pulling the image and building a sandbox, against ~3.6 s measured on Flex Consumption. Rolling back a bad deploy means uploading a previous release's `func-package.zip`, syncing triggers and restarting; the Container Apps were removed once all three environments were stable.

#### Windows shell hazard

`az` is a batch file, so cmd.exe re-parses the command line after PowerShell strips the quotes. Values containing `|`, `&` or `()` break and are not reliably escapable — passing plainly is rejected, and embedded quotes can return exit 0 while doing nothing. Secrets travel in the deployment parameter file (BOM-less UTF-8), and `--query` expressions avoid parentheses.

**Azurite must be running for anything that touches storage** — it is the whole persistence tier, so the API fails to start without it and the symptom is a port timeout. Compose owns the single definition: `docker compose -p ccdiary -f src/api/docker-compose.yml up -d azurite`.

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

1. **Setup** — Run `scripts/setuplocal.ps1` for initial environment setup
2. **Format** — `dotnet format .\ccDiary.sln` (API) / `npm run lint` (UI)
3. **Build** — `dotnet build ccDiary.sln` (API) / `npm run build` (UI)
4. **Run** — `dotnet run --project ccDiaryApi\ccDiaryApi.csproj` (API) / `npm run dev` (UI)
5. **Test** — `dotnet test ccDiary.sln -c Release ...` (API) / `npm run test:ci` (UI)
6. **Integration Tests** — Ensure both API and UI are running, then `npm run test:e2e`
7. **Check Branch Coverage** — Must be >85% overall and >85% on branches (SonarCloud gate)

## Git Workflow
- Branch naming: lowercase and `-` separated using only `a-z`and `0-9`
- Commit format: `type: description` (feat, fix, refactor, test, docs, feat!), breaking changes have type `feat!` or contain `BREAKING CHANGE` 
- Always create a branch before changes
- Run tests before committing

## Local Configuration

Sensitive values are never committed. Override them via:
- **User secrets** (`dotnet user-secrets`) for local `dotnet run`
- **Environment variables** for containers

### Required configuration keys

| Key | Purpose |
|---|---|
| `Storage:AccountName` | Storage account; the Container App uses its managed identity, so no secret is stored |
| `Storage:ConnectionString` | Used instead for Azurite locally |
| `Entra:ClientId` | Entra ID app registration client ID |
| `Entra:TenantId` | Entra ID tenant ID |
| `Entra:ApplicationIdUri` | Entra ID application ID URI |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP collector base URL (optional — OTel is disabled when absent) |
| `OTEL_EXPORTER_OTLP_HEADERS` | Comma-separated `key=value` auth headers for OTLP (e.g. Grafana Cloud token) |

### OpenTelemetry

OTel is configured in `OpenTelemetryExtensions.cs`. When `OTEL_EXPORTER_OTLP_ENDPOINT` is set it exports:
- **Traces** — ASP.NET Core, HttpClient, and the Azure SDKs via AddSource("Azure.*") → `{endpoint}/v1/traces`
- **Metrics** — ASP.NET Core, HttpClient, runtime instrumentation → `{endpoint}/v1/metrics`
- **Logs** — Serilog OTLP sink → `{endpoint}/v1/logs`

Signal paths are always appended explicitly because the SDK disables auto-append when the endpoint is set programmatically. The exporter uses HTTP/Protobuf with a **simple processor** (not batch) to handle scale-to-zero environments where the process may terminate before a batch flush.

Tracing excludes `/swagger`, `/actuator`, `/api/assembly-info`, and `/health` paths, and filters out low-value SQL probe queries (e.g. `SELECT 1`).

## Infrastructure as Code (Bicep)

`deploy/main.bicep` (subscription scope) → `resourceGroup.bicep` → `functionApp.bicep` (the API's host), plus `storageRoleAssignments.bicep` and `keyVaultRoleAssignment.bicep`. The two role modules exist because a role assignment's name must be computable at the start of a deployment and the principal ids they grant to are module outputs; assigning one inline fails with BCP120.

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

An accepted finding is marked “Won’t fix” on the issue in SonarCloud, not suppressed in code: an in-file `// NOSONAR` comment does nothing for the Bicep analyser, and a waiver in `sonar-project-infra.properties` would disable the rule for a whole file and hide future occurrences. Leave a comment in the template explaining the decision.

### SonarCloud Projects

SonarCloud organization (`cookingcode`)

| Project Key | Scope | Config |
|---|---|---|
| `cookingcode_ccDiary_api` | `src/api/` — C# API code | CLI args in `build-api` CI job |
| `cookingcode_ccDiary_ui` | `src/ui/src/`, `src/ui/tests/` | `sonar-project.properties` (repo root) |
| `cookingcode_ccDiary_infra` | `deploy/`, `scripts/`, `data/`, `*.ps1` | `sonar-project-infra.properties` (repo root) |

---

**Last Updated**: 2026-09-12
