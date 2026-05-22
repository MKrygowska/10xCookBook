---
project: 10xCookBook
researched_at: 2026-05-22T18:12:00+02:00
recommended_platform: Azure
runner_up: Render (Split-Hosting)
context_type: mvp
tech_stack:
  language: C# / TypeScript
  framework: ASP.NET Core / Angular
  runtime: .NET 8.0 / Node.js
---

## Recommendation

**Deploy on Azure.**

To satisfy the strict constraint of a **100% permanently free ($0/month)** hosting model where a **single platform** is responsible for hosting both the database and the application, Azure is the recommended infrastructure. This architecture utilizes **Azure Static Web Apps (SWA)** for the Angular 17 frontend, **Azure App Service (F1 Tier)** for the containerized C# .NET 8.0 API backend, and **Azure SQL Database (Serverless Free Offer)** as a persistent, non-expiring 32 GB SQL database. By employing Azure Static Web Apps' built-in routing proxy to forward `/api/*` to the App Service backend, the entire application operates under a single domain (with free SSL) and completely bypasses CORS security blockades, all for a flat $0/month. This leverages the developer's familiarity with Azure and provides a native, robust Entity Framework Core environment for the C# stack.

---

## Platform Comparison

To evaluate suitability for agentic development under a 100% free constraint, the candidate platforms were re-scored. The key requirement was to host both the application compute and a persistent, non-expiring database under a single platform for $0/month. Platforms without a permanent free tier or those whose free databases expire and get deleted (such as Render) were scored lower or failed the unified hosting filter.

| Platform | CLI-First | Managed / Serverless | Agent-Readable Docs | Stable Deploy API | MCP / Integration | Strategic Verdict |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Azure** | **PASS** | **PASS** | **PARTIAL** | **PASS** | **PARTIAL** | **PASS** (Recommended) |
| **Render** | **PASS** | **PASS** | **PASS** | **PASS** | **PASS** | **PARTIAL** (DB expires in 90 days; requires split) |
| **Koyeb** | **PASS** | **PASS** | **PASS** | **PASS** | **PARTIAL** | **PARTIAL** (DB limited to 5 active hours/mo) |
| **Railway** | **PASS** | **PASS** | **PASS** | **PASS** | **PASS** | **FAIL** (No permanent free tier; $5/mo Hobby) |
| **Fly.io** | **PASS** | **PARTIAL** | **PASS** | **PASS** | **PARTIAL** | **FAIL** (No permanent free tier; requires credit card) |
| **Cloudflare** | **PASS** | **PASS** | **PASS** | **PASS** | **PASS** | **FAIL** (V8 runtime cannot execute C# Kestrel API) |
| **Vercel** | **PASS** | **PASS** | **PASS** | **PASS** | **PASS** | **FAIL** (No native container or C# API execution) |
| **Netlify** | **PASS** | **PASS** | **PASS** | **PASS** | **PASS** | **FAIL** (No native container or C# API execution) |

### Platform Performance Summary

- **Azure (Static Web Apps + App Service F1 + SQL Database)**: Earned the winning recommendation by providing a completely free, unified hosting solution. It satisfies the database-and-compute single-host constraint. Azure SQL offers a lifetime free 32 GB serverless tier, which matches the C#/.NET stack perfectly. App Service F1 provides free container/code hosting (subject to 60 CPU mins/day), and Static Web Apps serves the Angular SPA for free with integrated proxying.
- **Render**: Scored high on DX and CLI operations. However, Render's free PostgreSQL database has a strict 90-day expiration limit, after which it is permanently deleted. This makes it impossible to host a persistent database for free under Render alone, requiring a split-hosting model (e.g., Render + Supabase).
- **Koyeb**: Offers container web service hosting and managed databases. However, Koyeb's free PostgreSQL tier is strictly limited to **5 hours of active database time per month** (~10 minutes per day), which is unviable for a working application.
- **Railway**: Provides excellent internal networking and MCP integration, but its free tier has been eliminated, and the $5/month Hobby plan violates the $0/month cost requirement.
- **Fly.io**: Offers Firecracker microVMs but has discontinued its stable free tier and has no managed database service, shifting backup and replication management to the developer.
- **Cloudflare / Vercel / Netlify**: These edge platforms provide outstanding free static site hosting for the Angular frontend, but cannot host the C# .NET 8.0 backend API.

---

### Shortlisted Platforms

#### 1. Azure (Static Web Apps + App Service F1 + SQL Serverless)
Azure won the primary recommendation because it is the only major cloud platform that allows hosting the frontend SPA, the containerized C# backend API, and a permanent, non-expiring database under a single platform for $0/month. The Serverless Azure SQL Database free tier is incredibly generous, offering 32 GB of persistent storage and 100,000 vCore seconds/month. When combined with Static Web Apps' CDN and App Service's F1 compute, the developer gains a fully integrated, zero-cost, enterprise-grade Microsoft stack that aligns with their pre-existing familiarity and C# native conventions.

#### 2. Render (Split-Hosting with Supabase)
Render is the runner-up. It offers an exceptional free Static Site service for Angular and a free Web Service container tier (512MB RAM, 15-minute inactivity sleep) for the C# API. However, because Render's free PostgreSQL database expires and is deleted after 90 days, the database must be hosted externally on a dedicated free provider like **Supabase** (which offers a persistent 500MB PostgreSQL free tier). While this split-hosting architecture is fully functional, it requires managing resources across two separate platform accounts (Render and Supabase) and configuring CORS/redirect proxies across distinct domains.

#### 3. Koyeb (Split-Hosting with Neon)
Koyeb represents the third-place alternative. It provides 1 GB of free RAM and 0.25 CPU for container hosting, which is larger than Render's free RAM limit. However, because Koyeb's free PostgreSQL database is limited to 5 active hours per month, it also requires a split-hosting topology with an external database provider like **Neon** (offering a persistent 0.5 GB serverless PostgreSQL database). The CLI tooling is stable, but the split configuration introduces similar multi-account overhead and CORS management challenges.

---

## Anti-Bias Cross-Check: Azure

### Devil's Advocate — Weaknesses

1. **Daily Compute Limitation**: The App Service F1 plan has a strict quota of **60 CPU minutes per day**. While adequate for light MVP testing, it can be instantly exhausted by background loops, heavy Swagger API exploration, or runaway client-side fetch requests. Once exhausted, the backend is suspended for the rest of the day, returning HTTP 403 errors.
2. **Double Cold-Start Penalty**: The App Service container scales to zero when idle, and the Serverless Azure SQL database auto-pauses after inactivity. A user accessing the app after an idle period faces a double cold start: **30 seconds** for the App Service container to boot and **10–15 seconds** for the database server to wake up, resulting in a frustrating initial latency of up to 45 seconds.
3. **Microsoft SQL Server Transition**: Changing the database from PostgreSQL to SQL Server means Entity Framework Core must be configured to use `Microsoft.EntityFrameworkCore.SqlServer`. PostgreSQL-specific features like `ILIKE` or `JSONB` are not supported, requiring the developer to use standard, database-agnostic LINQ queries.
4. **App Service F1 Custom Domain Restriction**: The App Service F1 free plan does not allow custom SSL domain bindings. We must route all traffic through Azure Static Web Apps (which does support free custom domains) and proxy API requests.
5. **Slow SWA Proxy Timeout**: The Azure Static Web Apps proxy has a hard 30-second timeout. If the database and App Service are both cold-starting and take longer than 30 seconds to return the initial response, SWA will abort and return a `504 Gateway Timeout`.

### Pre-Mortem — How This Could Fail

The developer successfully deployed the 10xCookBook MVP on Azure's free tiers. The first few days of demoing went perfectly. However, during a frontend update, the developer introduced a minor bug in the Angular recipe search component: the HTTP client was set to automatically retry failed requests every 500ms without a backoff ceiling. 

A beta tester opened the application in a browser tab and went to lunch. Due to a temporary cold-start delay, the initial request timed out, triggering the rapid 500ms retry loop in the background. While the tester was away, the Angular app sent thousands of requests to the C# API. The App Service container worked continuously to process the requests and query the database, exhausting the F1 plan's 60 CPU minutes quota in less than 35 minutes. By 1:00 PM, the backend was completely suspended. 

When the developer attempted to showcase the MVP to a critical stakeholder at 2:00 PM, the application's login and search failed immediately with an HTTP 403 Quota Exceeded error. Unable to resume the service without upgrading to a paid B1 App Service Plan ($12/month), the showcase was aborted, and the stakeholder lost confidence in the stability of the application.

### Unknown Unknowns

- **Serverless SQL Auto-Pause Timeout**: By default, Azure SQL Serverless databases are configured with an auto-pause delay of 1 hour. If a query is run once every 55 minutes, the database never pauses and continuously consumes vCore seconds, which will quickly exhaust the 100,000 free vCore seconds monthly quota. It is critical to adjust the auto-pause delay to its minimum setting (60 minutes) and ensure no continuous background telemetry or health check pings are hitting the database.
- **EF Core Database Creation Privileges**: Azure SQL Serverless free accounts have restricted permissions. Running EF Core automatic migrations (`context.Database.Migrate()`) at application startup can fail if the migration script attempts to create tables or schemas without explicit DBO privileges. Database schemas must be generated locally or deployed via migration scripts with standard credentials.
- **SWA Header Stripping**: Azure Static Web Apps strips or rewrites certain custom HTTP headers (such as CORS headers or custom authentication headers) when proxying requests to an external App Service. If the C# backend relies on custom non-standard headers for session tracking, they will be discarded, causing silent authentication failures.

---

## Operational Story

- **Preview deploys**: Automatically generated for the Angular frontend on every GitHub Pull Request via Azure Static Web Apps' native integration; backend API preview environments are not automatically provisioned on the F1 free tier and must share a single dev deployment slot.
- **Secrets**: Encrypted at rest and managed securely within the Azure App Service "Environment Variables" portal configuration; injected directly into the C# application environment at runtime and never committed to Git.
- **Rollback**: Performed via the GitHub Actions deployment panel by redeploying a previous successful workflow run, or through the Azure CLI by deploying a previous zip package; typical rollbacks complete in under 30 seconds.
- **Approval**: Scaling up App Service tiers, deleting the Azure SQL server, or modifying billing boundaries requires a manual human gate via the Azure Portal; standard deployments and environment variable updates can be performed unattended by the agent.
- **Logs**: Tailed programmatically in the terminal using the Azure CLI command: `az webapp log tail --name <app-name> --resource-group <group-name>`, providing real-time diagnostics of the C# application.

---

## Risk Register

| Risk | Source | Likelihood | Impact | Mitigation |
| :--- | :--- | :---: | :---: | :--- |
| **Daily CPU Quota Exhaustion** | Pre-mortem | Medium | High | Implement strict request rate-limiting on the API, and write defensive frontend code with debouncing and retry caps. |
| **SWA Proxy 504 Timeout** | Devil's advocate | High | Medium | Configure the Azure SQL Serverless database auto-pause delay and ensure the frontend displays a clear, elegant loading spinner during cold starts. |
| **vCore Seconds Quota Depletion** | Unknown unknowns | Medium | High | Set the database auto-pause delay strictly to 60 minutes and disable all automatic background pings or active health-check probes. |
| **EF Core Migration Schema Crash** | Unknown unknowns | Medium | Medium | Generate the database migration SQL scripts locally and apply them using Azure Query Editor or SQL Server Management Studio (SSMS) instead of automated runtime migrations. |
| **Header Stripping Auth Failure** | Unknown unknowns | Low | High | Use standard HTTP `Authorization: Bearer <JWT>` headers for authentication, which are fully supported and passed natively by the SWA proxy. |

---

## Getting Started

Follow these concrete steps to provision and deploy the 10xCookBook full-stack application on Azure's free tiers.

### Step 1: Install the Azure CLI and Tools
Install the Azure CLI and the Static Web Apps CLI on your local Windows system:
```powershell
# Install Azure CLI
winget install -e --id Microsoft.AzureCLI

# Install SWA CLI for local proxy testing
npm install -g @azure/static-web-apps-cli
```
*Run `az login` in your terminal to authenticate with your Azure account.*

### Step 2: Create a Resource Group
Create an isolated logical container for all your resources in your preferred region:
```powershell
az group create --name 10x-cookbook-rg --location eastus
```

### Step 3: Provision the Azure SQL Server and Free Database
Provision a logical SQL Server and create the free Serverless database:
```powershell
# Create SQL Server
az sql server create --name cookbook-sqlserver-unique --resource-group 10x-cookbook-rg --location eastus --admin-user dbadmin --admin-password "YourStrongPassword123!"

# Provision the free Serverless database (GP_S_Gen5_1 compute tier with auto-pause enabled)
az sql db create --name cookbook-db --resource-group 10x-cookbook-rg --server cookbook-sqlserver-unique --edition GeneralPurpose --family Gen5 --capacity 1 --compute-model Serverless --auto-pause-delay 60
```

### Step 4: Provision App Service Plan and Web App (C# API Backend)
Create a free App Service plan (F1) and create the Web App:
```powershell
# Create Free F1 App Service Plan
az appservice plan create --name cookbook-asp --resource-group 10x-cookbook-rg --sku F1 --is-linux

# Create Web App for .NET 8.0 C# Backend
az webapp create --name cookbook-api-unique --resource-group 10x-cookbook-rg --plan cookbook-asp --runtime "DOTNET|8.0"
```

### Step 5: Configure Connection Strings and Settings
Inject the database connection string and standard environment variables into the App Service:
```powershell
# Set database connection string in App Service environment
az webapp config connection-string set --name cookbook-api-unique --resource-group 10x-cookbook-rg --connection-string-type SQLServer --settings DefaultConnection="Server=tcp:cookbook-sqlserver-unique.database.windows.net,1433;Initial Catalog=cookbook-db;Persist Security Info=False;User ID=dbadmin;Password=YourStrongPassword123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

### Step 6: Configure SWA API Proxy for Frontend
In your frontend directory, create a `staticwebapp.config.json` file to route all `/api/*` traffic to the App Service backend, bypassing CORS and allowing a unified domain:
```json
{
  "navigationFallback": {
    "rewrite": "/index.html"
  },
  "routes": [
    {
      "route": "/api/*",
      "rewrite": "https://cookbook-api-unique.azurewebsites.net/api/*"
    }
  ]
}
```

---

## Out of Scope

The following were not evaluated in this research:
- Docker container optimizations and manual Dockerfile writing.
- Pinned GitHub Actions workflow yaml customization.
- Production-scale database sharding, geo-replication, or automated disaster recovery (DR) procedures.
