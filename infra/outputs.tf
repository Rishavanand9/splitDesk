# These values are printed after `terraform apply`.
# Copy the api_url into your frontend .env.production file.

output "api_url" {
  description = "URL of the deployed C# API. Set as VITE_API_URL in frontend/.env.production"
  value       = "https://${azurerm_linux_web_app.api.default_hostname}"
}

output "frontend_url" {
  description = "URL of the deployed React app"
  value       = "https://${azurerm_static_web_app.frontend.default_host_name}"
}

output "swa_deployment_token" {
  description = "Token used by Azure DevOps / GitHub Actions to deploy the React app to SWA"
  value       = azurerm_static_web_app.frontend.api_key
  sensitive   = true  # won't print in terminal — use: terraform output -raw swa_deployment_token
}

output "resource_group_name" {
  description = "Resource group — useful for az CLI commands"
  value       = azurerm_resource_group.main.name
}
