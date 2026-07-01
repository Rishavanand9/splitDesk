variable "location" {
  description = "Azure region for all resources"
  type        = string
  default     = "uksouth"
}

variable "environment" {
  description = "Environment name — used in resource naming"
  type        = string
  default     = "prod"
}

variable "app_name" {
  description = "Base name for all resources"
  type        = string
  default     = "splitdesk"
}

variable "sku_name" {
  description = "App Service Plan SKU. B1 = Basic (cheapest paid). F1 = Free (no custom domain/SSL)."
  type        = string
  default     = "B1"
}

# Populated by Terraform output after first apply — paste the SWA URL here
variable "frontend_origin" {
  description = "URL of the Static Web App — used in CORS config for the API"
  type        = string
  default     = ""
}
