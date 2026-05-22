# Deploy Plan — 100% Free Unified Azure Infrastructure

This plan defines the step-by-step deployment procedure for the **10xCookBook** MVP. It integrates our **Angular 17 frontend** with the **ASP.NET Core C# API backend** and the **Serverless Azure SQL Database**, completely on Azure's $0/month free tiers.

## Core Topology
- **Frontend SPA**: Azure Static Web Apps (Free SKU) — serving compiled Angular 17.
- **Backend API**: Azure App Service (F1 Windows Free Tier, Sweden Central) — hosting the C# Web API.
- **Database**: Azure SQL Serverless (GP_S_Gen5_1, Sweden Central) — persistent 32 GB SQL Database.
- **API Proxy Router**: Azure Static Web Apps built-in rewrite engine (`staticwebapp.config.json`) routing `/api/*` requests to the App Service backend, completely bypassing CORS.

---

## Current Provisioned State (Verified)
- **Resource Group**: `10x-cookbook-rg`
- **SQL Server**: `cookbook-sql-irisuel-sec` (Sweden Central)
- **Database**: `cookbook-db` (Sweden Central)
- **App Service Plan**: `cookbook-asp` (Windows F1 SKU, Sweden Central)
- **Web App (Backend C#)**: `cookbook-api-unique` (Sweden Central)
- **Database Connection String**: Wired inside `cookbook-api-unique` Web App under the setting key `DefaultConnection` (pointing to `cookbook-sql-irisuel-sec.database.windows.net`).

---

## Step-by-Step Execution Plan

### Phase 1: Local Compile Checks & Preparation
1. **Frontend Compilation Check**:
   - Run local Angular compilation:
     ```powershell
     cd frontend
     npm run build
     ```
   - Verify that output is successfully generated at `frontend/dist/bootstrap-scaffold/browser/` and includes the registered `staticwebapp.config.json` configuration file.
2. **Backend Compilation Check**:
   - Run local ASP.NET Core compilation:
     ```powershell
     cd backend
     dotnet build
     ```
   - Verify that compilation succeeds with 0 errors.

### Phase 2: Static Web App Provisioning
1. **Create SWA Resource**:
   - Provision a Free Static Web App named `cookbook-swa-unique` in the existing resource group `10x-cookbook-rg`.
   - Control plane region set to `westeurope` (globally distributed CDN is edge-rendered, control plane in West Europe).
   - Command:
     ```powershell
     & "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd" staticwebapp create --name cookbook-swa-unique --resource-group 10x-cookbook-rg --location westeurope --branch main --sku Free
     ```

### Phase 3: SWA Deployment Token & Local Deployment
1. **Retrieve SWA Secrets**:
   - Query Azure CLI for the deployment token (`properties.apiKey`):
     ```powershell
     & "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd" staticwebapp secrets list --name cookbook-swa-unique --resource-group 10x-cookbook-rg --query properties.apiKey --output tsv
     ```
2. **Deploy Frontend SPA**:
   - Deploy the compiled Angular browser package using the globally installed SWA CLI:
     ```powershell
     & npx swa deploy ./frontend/dist/bootstrap-scaffold/browser --api-location "" --env production --deployment-token "<DEPLOYS_TOKEN>"
     ```

### Phase 4: Backend API Publish & Deployment
1. **Publish API Package**:
   - Compile and package the ASP.NET Core API for App Service deployment:
     ```powershell
     cd backend
     dotnet publish -c Release -o ./publish
     ```
2. **ZIP backend folder**:
   - Compress the published output to prepare a standard ZIP deployment package:
     ```powershell
     Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force
     ```
3. **Deploy ZIP package to App Service**:
   - Push the ZIP package directly to the Free Windows App Service using ZIP Deploy:
     ```powershell
     & "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd" webapp deploy --name cookbook-api-unique --resource-group 10x-cookbook-rg --src-path ./publish.zip --type zip --async false
     ```

### Phase 5: Verification & Cold-Start Initialisation
1. **Retrieve SWA Hostname**:
   - Get the assigned hostname of the Static Web App:
     ```powershell
     & "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd" staticwebapp show --name cookbook-swa-unique --resource-group 10x-cookbook-rg --query defaultHostname --output tsv
     ```
2. **End-to-End Verification Calls**:
   - Send an initial request to trigger Web App wake-up and database initialization:
     ```powershell
     Invoke-RestMethod -Uri "https://<SWA_HOST>/weatherforecast"
     ```
   - Verify that the response returns the compiled WeatherForecast API JSON successfully.
   - Access the homepage and verify that the Angular site loads with zero console errors.

---

## Security & Access Boundary
- **Secrets Management**: DB credentials (`Password`) live exclusively in the encrypted Azure App Service connection settings; they are never exposed in client bundles or committed to Git.
- **Minimal Permissions**: Standard SWA and ZIP CLI deployments run using target-scoped tokens (SWA API Key) or authenticated Azure CLI session tokens, avoiding the creation of persistent custom master keys.
