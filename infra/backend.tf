# Remote state — stores terraform.tfstate in Azure Blob so any machine can run terraform.
# This also enables state locking (blob lease) so two people can't apply at the same time.
# Run ONCE before anything else:
#   az group create --name rg-splitdesk-tfstate --location uksouth
#   az storage account create --name stsplitdesktf --resource-group rg-splitdesk-tfstate --sku Standard_LRS
#   az storage container create --name tfstate --account-name stsplitdesktf
terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.110"
    }
  }

  backend "azurerm" {
    resource_group_name  = "rg-splitdesk-tfstate"
    storage_account_name = "stsplitdesktf"
    container_name       = "tfstate"
    key                  = "splitdesk.tfstate"
  }
}

provider "azurerm" {
  features {}
}
