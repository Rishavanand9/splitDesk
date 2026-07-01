# ADR-004 — Use Terraform for Azure Infrastructure Provisioning

**Date:** 2026-07-01  
**Status:** Accepted

---

## Context

splitDesk needs to be deployed to Azure. The NatWest JD lists "Terraform/Bicep is a plus" for the infrastructure-as-code requirement. We need a way to provision and destroy Azure resources repeatably without manual portal clicks.

Options considered:
- **Terraform (azurerm provider)** — cloud-agnostic HCL, large community, explicitly on JD
- **Azure Bicep** — Azure-native, also on JD, JSON/ARM successor
- **Azure CLI scripts** — imperative, not idempotent, hard to version
- **Azure Portal (manual)** — not reproducible, no code artifact to show in interview
- **Pulumi** — code-first IaC (TypeScript/C#), not on JD

## Decision

Use **Terraform** with the `azurerm` provider and a **remote state backend** in Azure Blob Storage.

## Consequences

**Positive:**
- Directly on the JD ("Terraform/Bicep is a plus" — we treat it as a must for interview credibility)
- `terraform plan` gives a dry-run preview before any changes — safe for interviews to demo
- `terraform destroy` tears down everything cleanly — no surprise Azure charges
- Remote state in Azure Blob (with state locking) mirrors exactly how production teams use Terraform — a favourite interview scenario question
- HCL is readable and concise for this use case — fewer lines than equivalent Bicep for simple resources
- Cloud-agnostic knowledge transfers beyond Azure

**Negative:**
- State management adds complexity — must initialise remote backend before first `apply`
- `azurerm` provider version pinning matters — breaking changes between major versions
- Terraform requires Azure credentials (`az login` or service principal) — slightly more setup than Bicep's native ARM integration
- State locking via Storage Account blob leases is correct but can leave stale locks if `apply` crashes (resolved with `terraform force-unlock`)

## State Locking — Why It Matters (Interview Gold)

A common interview scenario question: *"What happens if two engineers run `terraform apply` at the same time?"*

**Answer:** Terraform acquires a lock on the state file before making changes. With the Azure Blob backend, this is implemented as a blob lease. The second `apply` waits until the lock is released. Without locking (e.g., using local state), both applies would read the same state and could produce conflicting resource changes or corrupt the state file.

## Terraform vs Bicep Comparison

| | Terraform | Bicep |
|---|---|---|
| Syntax | HCL | JSON-like DSL |
| Scope | Multi-cloud | Azure only |
| State | Explicit (remote blob) | ARM handles state natively |
| Plan/preview | `terraform plan` | `what-if` deployment |
| Modules | First-class | Modules via spec |
| Interview prevalence | Very common | Growing, Azure-specific teams |

## Interview Talking Point

> "I used Terraform over Bicep because the JD mentioned both and Terraform's cloud-agnostic nature is more widely transferable. I configured remote state in Azure Blob with locking — which mirrors production setups and demonstrates understanding of concurrent apply scenarios. I also used modules to separate the App Service and Static Web App concerns, following the single responsibility principle even in infrastructure code."
