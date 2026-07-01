locals {
  # Consistent naming: splitdesk-prod, stsplitdeskprod, etc.
  prefix = "${var.app_name}-${var.environment}"
}

# ── Resource Group ────────────────────────────────────────────────────────────
resource "azurerm_resource_group" "main" {
  name     = "rg-${local.prefix}"
  location = var.location

  tags = {
    project     = "splitDesk"
    environment = var.environment
    managed_by  = "terraform"
  }
}

# ── App Service Plan (shared by the API web app) ─────────────────────────────
# Linux B1 = ~£10/month. Run `terraform destroy` between sessions to avoid charges.
resource "azurerm_service_plan" "main" {
  name                = "asp-${local.prefix}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = var.sku_name
}

# ── C# API — Linux Web App ───────────────────────────────────────────────────
resource "azurerm_linux_web_app" "api" {
  name                = "api-${local.prefix}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id

  site_config {
    # .NET 8 runtime stack on Linux
    application_stack {
      dotnet_version = "8.0"
    }
    # Allow requests from the React SWA and local dev
    cors {
      allowed_origins = compact([
        "https://${azurerm_static_web_app.frontend.default_host_name}",
        "http://localhost:5173",
        "http://localhost:5174",
        var.frontend_origin,
      ])
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT" = "Production"
    # Application Insights (optional — add connection string from Key Vault for prod)
    # "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.main.connection_string
  }

  tags = azurerm_resource_group.main.tags
}

# ── React Frontend — Azure Static Web App ────────────────────────────────────
# Free tier — no App Service Plan needed. CDN-backed. Purpose-built for SPAs.
resource "azurerm_static_web_app" "frontend" {
  name                = "swa-${local.prefix}"
  resource_group_name = azurerm_resource_group.main.name
  location            = "westeurope" # SWA has limited region availability

  sku_tier = "Free"
  sku_size = "Free"

  tags = azurerm_resource_group.main.tags
}
