# Deployment Architecture — splitDesk

---

## Azure Infrastructure Diagram

```mermaid
graph TB
  subgraph "Developer Machine"
    DEV[Local Dev\nnpm run dev\ndotnet run]
    TF[Terraform CLI\nterraform apply]
  end

  subgraph "Azure Subscription"
    subgraph "Resource Group: rg-splitdesk-prod"
      subgraph "App Service Plan: asp-splitdesk"
        API[Linux Web App\napi-splitdesk\nASP.NET Core API]
      end
      SWA[Static Web App\nswa-splitdesk\nReact SPA]
      subgraph "Storage Account: stsplitdesktfstate"
        BLOB[Blob Container\nterraform-state\nterraform.tfstate]
      end
    end
  end

  TF -->|provisions| API
  TF -->|provisions| SWA
  TF -->|remote state| BLOB

  Browser[User's Browser] -->|HTTPS| SWA
  SWA -->|API calls HTTPS| API
  DEV -.->|local dev only| API
```

---

## Terraform Resource Map

```mermaid
graph LR
  RG[azurerm_resource_group\nrg-splitdesk-prod]

  ASP[azurerm_service_plan\nasp-splitdesk\nLinux B1]
  API[azurerm_linux_web_app\napi-splitdesk]
  SWA[azurerm_static_web_app\nswa-splitdesk]
  SA[azurerm_storage_account\nstsplitdesktfstate]
  SC[azurerm_storage_container\nterraform-state]

  RG --> ASP
  RG --> SWA
  RG --> SA
  ASP --> API
  SA --> SC

  BACKEND["Terraform Backend\n(remote state in blob)"] -.-> SC
```

---

## Deployment Pipeline (Azure DevOps)

```mermaid
flowchart LR
  subgraph "CI — Build Stage"
    A[git push] --> B[Trigger pipeline]
    B --> C[dotnet restore]
    C --> D[dotnet build]
    D --> E[dotnet test\nwith Coverlet]
    E --> F{Coverage ≥ 70%?}
    F -->|No| FAIL1[❌ Fail build]
    F -->|Yes| G[dotnet publish\nzip artifact]
    G --> H[npm install\nnpm run build]
    H --> I[Publish frontend artifact]
  end

  subgraph "CD — Deploy Stage"
    I --> J[Download artifacts]
    J --> K[terraform init\nterraform plan]
    K --> L{Manual approval}
    L -->|Approved| M[terraform apply]
    M --> N[az webapp deploy\nAPI zip]
    N --> O[swa deploy\nfrontend dist]
    O --> P[✅ Live on Azure]
  end
```

---

## Terraform File Structure

```
splitDesk/
└── infra/
    ├── main.tf          ← provider config + resource group
    ├── variables.tf     ← input variables (location, app name, SKU)
    ├── outputs.tf       ← API URL, SWA URL
    ├── backend.tf       ← remote state config (Azure Blob)
    └── modules/
        ├── app_service/
        │   ├── main.tf
        │   ├── variables.tf
        │   └── outputs.tf
        └── static_web_app/
            ├── main.tf
            ├── variables.tf
            └── outputs.tf
```

---

## Environment Variables

### Frontend (`frontend/.env.production`)

```env
VITE_API_URL=https://api-splitdesk.azurewebsites.net
```

### Backend (Azure App Service Application Settings)

| Setting | Value | Notes |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Disables Swagger in prod |
| `AllowedOrigins__0` | `https://swa-splitdesk.azurestaticapps.net` | CORS — frontend origin |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | `<from Key Vault>` | Optional — telemetry |

---

## Network Flow

```
User Browser
    │
    │ HTTPS (port 443)
    ▼
Azure Static Web App (CDN-backed)
    → Serves React bundle (HTML/JS/CSS)
    │
    │ HTTPS (port 443) — API call from browser JS
    ▼
Azure App Service (Linux, .NET 8)
    → Runs ASP.NET Core process
    → Handles POST /api/bills/split
    → Returns JSON response
```

**Key point:** The browser talks to *two* Azure services — one for the static files (SWA), one for the API (App Service). The SWA does not proxy to the API; the React app calls the API directly using the `VITE_API_URL` env variable.

---

## Cost Estimate (Minimal Dev Setup)

| Resource | SKU | Est. Monthly Cost |
|---|---|---|
| App Service Plan (B1) | Basic | ~£10/month |
| Static Web App | Free tier | £0 |
| Storage Account (state) | LRS Standard | ~£0.02/month |
| **Total** | | **~£10/month** |

Run `terraform destroy` between sessions to avoid charges during development.
