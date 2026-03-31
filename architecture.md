# ccDiary — Architecture

ccDiary is deployed across three isolated Azure environments (**dev**, **staging**, **prod**), each containing an identical stack of Azure resources. All environments share common external services for identity, observability, and code quality.

---

## Azure Resource Structure

The diagram below shows the Azure services deployed in each environment and how they connect. Replace `{env}` with `dev`, `staging`, or `prod`.

```mermaid
architecture-beta
    service user["👤 Users"]
    service entra(azure:entra-id)["Microsoft Entra ID\nOAuth 2.0 · JWT Bearer\nApp: ccDiaryApi"]
    service ghcr(cloud)["GitHub Actions · GHCR\nghcr.io/sinclapa/ccdiary-api\naz containerapp update"]
    service grafana(cloud)["Grafana Cloud\nOTLP HTTP/Protobuf\ntraces · metrics · logs"]

    group rg(azure:resource-groups)["rg-ccdiary-{env}"]
        service swa(azure:static-web-apps)["stapp-ccdiary-{env}\n.azurestaticapps.net\nVue.js SPA · MSAL auth"]
        group cae(azure:container-apps-environments)["cae-ccdiary-{env}"]
            service ca(azure:container-apps)["ca-ccdiary-{env}\nASP.NET Core 8 · port 8080\n0.25 vCPU · 0.5 GiB · 0–1 replicas"]
        end
        service logs(azure:log-analytics-workspaces)["logs-ccdiary-{env}\n30-day retention\nAzure Monitor"]
        service db(azure:sql-database)["sql-ccdiary-{env}.database.windows.net\nDB: sqldb-ccdiary-{env}\nGP_S_Gen5_1 serverless · Managed Identity"]
    end

    user:R --> L:swa
    entra:B --> T:swa
    swa:B --> T:ca
    ghcr:R --> L:ca
    ca:B --> T:db
    ca:R --> L:logs
    grafana:B --> T:logs
```

---

## Multi-Environment Deployment Overview

Three independent deployments, each in its own Azure Resource Group. CI/CD deploys to dev and staging automatically; prod requires a tagged GitHub Release.

```mermaid
flowchart TB
    subgraph GH["⚙️ GitHub"]
        direction LR
        Actions["GitHub Actions\nbuild-and-test.yml\nrelease-prod.yml"]
        GHCR["📦 GHCR\nghcr.io/sinclapa/ccdiary-api:{ver}"]
        Actions --> GHCR
    end

    subgraph ExtSvc["Shared External Services"]
        direction LR
        Entra["🔑 Microsoft Entra ID"]
        Grafana["📊 Grafana Cloud OTLP"]
        Sonar["🔍 SonarCloud\ncookingcode org"]
    end

    subgraph DevEnv["☁️ rg-ccdiary-dev  ·  trigger: push to any branch"]
        direction LR
        DevSWA["stapp-ccdiary-dev\n.azurestaticapps.net"]
        DevCA["ca-ccdiary-dev\n(cae-ccdiary-dev)"]
        DevDB[("sql-ccdiary-dev\nsqldb-ccdiary-dev")]
        DevLog["logs-ccdiary-dev"]
        DevSWA --> DevCA --> DevDB
        DevCA --> DevLog
    end

    subgraph StgEnv["☁️ rg-ccdiary-staging  ·  trigger: push to main"]
        direction LR
        StgSWA["stapp-ccdiary-staging\n.azurestaticapps.net"]
        StgCA["ca-ccdiary-staging\n(cae-ccdiary-staging)"]
        StgDB[("sql-ccdiary-staging\nsqldb-ccdiary-staging")]
        StgLog["logs-ccdiary-staging"]
        StgSWA --> StgCA --> StgDB
        StgCA --> StgLog
    end

    subgraph ProdEnv["☁️ rg-ccdiary-prod  ·  trigger: GitHub Release tag v*"]
        direction LR
        ProdSWA["stapp-ccdiary-prod\n+ custom domain"]
        ProdCA["ca-ccdiary-prod\n(cae-ccdiary-prod)"]
        ProdDB[("sql-ccdiary-prod\nsqldb-ccdiary-prod")]
        ProdLog["logs-ccdiary-prod"]
        ProdSWA --> ProdCA --> ProdDB
        ProdCA --> ProdLog
    end

    GHCR -->|az containerapp update| DevCA
    GHCR -->|az containerapp update| StgCA
    GHCR -->|az containerapp update| ProdCA
    Actions -->|swa deploy| DevSWA & StgSWA & ProdSWA
    Actions -.->|scan| Sonar

    DevSWA & StgSWA & ProdSWA -.->|MSAL login| Entra
    DevCA  & StgCA  & ProdCA  -.->|JWT validation| Entra
    DevCA  & StgCA  & ProdCA  -.->|OTLP| Grafana
```

---

## Instance Details

Resource names follow the pattern `{prefix}-ccdiary-{env}`. Container App FQDNs include an Azure-generated random suffix.

| Component | Dev | Staging | Prod |
|---|---|---|---|
| **Resource Group** | `rg-ccdiary-dev` | `rg-ccdiary-staging` | `rg-ccdiary-prod` |
| **Static Web App** | `stapp-ccdiary-dev.azurestaticapps.net` | `stapp-ccdiary-staging.azurestaticapps.net` | `stapp-ccdiary-prod.azurestaticapps.net` + custom domain |
| **Container App** | `ca-ccdiary-dev.{suffix}.azurecontainerapps.io` | `ca-ccdiary-staging.{suffix}.azurecontainerapps.io` | `ca-ccdiary-prod.{suffix}.azurecontainerapps.io` |
| **Container App Env** | `cae-ccdiary-dev` | `cae-ccdiary-staging` | `cae-ccdiary-prod` |
| **SQL Server FQDN** | `sql-ccdiary-dev.database.windows.net` | `sql-ccdiary-staging.database.windows.net` | `sql-ccdiary-prod.database.windows.net` |
| **SQL Database** | `sqldb-ccdiary-dev` | `sqldb-ccdiary-staging` | `sqldb-ccdiary-prod` |
| **Log Analytics** | `logs-ccdiary-dev` | `logs-ccdiary-staging` | `logs-ccdiary-prod` |
| **Container Image** | `ghcr.io/sinclapa/ccdiary-api:{semver}` | `ghcr.io/sinclapa/ccdiary-api:{semver}` | `ghcr.io/sinclapa/ccdiary-api:{semver}` |
| **GitHub Environment** | `dev` | `staging` | `prod` |
| **Deploy Trigger** | Push to any non-main branch | Push / merge to `main` | GitHub Release tag `v*` |
| **SQL Auth** | Managed Identity (Entra-only) | Managed Identity (Entra-only) | Managed Identity (Entra-only) |
| **SQL SKU** | GP_S_Gen5_1 serverless | GP_S_Gen5_1 serverless | GP_S_Gen5_1 serverless |
| **CA Resources** | 0.25 vCPU · 0.5 GiB | 0.25 vCPU · 0.5 GiB | 0.25 vCPU · 0.5 GiB |
| **CA Scale** | 0–1 replicas | 0–1 replicas | 0–1 replicas |
| **OTLP Endpoint** | `otlp-gateway-prod-gb-south-1.grafana.net/otlp` | `otlp-gateway-prod-gb-south-1.grafana.net/otlp` | `otlp-gateway-prod-gb-south-1.grafana.net/otlp` |
| **API Actuator** | `https://{ca-fqdn}/actuator` | `https://{ca-fqdn}/actuator` | `https://{ca-fqdn}/actuator` |
| **Swagger UI** | `https://{ca-fqdn}/swagger` | `https://{ca-fqdn}/swagger` | `https://{ca-fqdn}/swagger` |

---

## Local Development

```mermaid
flowchart LR
    Browser(["👤 Developer\nBrowser"])

    subgraph Local["💻 Developer Machine"]
        ViteDev["⚡ Vite Dev Server\nlocalhost:8080\nnpm run dev"]
        API["🔧 ASP.NET Core API\nlocalhost:7183 HTTPS\ndotnet run"]
        subgraph Docker["🐳 Docker Compose"]
            SQL[("🗄️ SQL Server 2022\nlocalhost:51433\nmssql/server:2022-latest")]
        end
    end

    Browser -->|"http://localhost:8080"| ViteDev
    ViteDev -->|"REST /api/v1/"| API
    API -->|"EF Core\nauto-migrate on startup"| SQL
```

| Component | Value |
|---|---|
| UI | `http://localhost:8080` |
| API | `https://localhost:7183` |
| Swagger | `https://localhost:7183/swagger` |
| SQL port | `51433` (mapped from container `1433`) |
| Auth config | User secrets (`Entra:ClientId`, `Entra:TenantId`) |
| DB password | `.env` → `SA_PASSWORD` |
| `VITE_ENVIRONMENT` | `local` (from `.env.dev.local`) |
