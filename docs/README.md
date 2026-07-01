# splitDesk — Documentation Index

> Fair bill splitting, item by item.

## What is splitDesk?

splitDesk is a full-stack web application that splits a bill fairly among participants based on what each person actually consumed — not just dividing the total equally. Tax and tip are distributed proportionally to each person's share of the pre-tax subtotal.

---

## Document Map

| Document | Purpose |
|---|---|
| [System Design](./system-design.md) | End-to-end system architecture, data models, API contract, algorithm |
| [Engineering Spec](./engineering-spec.md) | Requirements, constraints, acceptance criteria, non-functional requirements |
| [Architecture Overview](./architecture/overview.md) | High-level component diagram and technology choices |
| [Data Flow](./architecture/data-flow.md) | Request lifecycle and state flow diagrams |
| [Deployment Architecture](./architecture/deployment.md) | Azure infrastructure, Terraform topology, CI/CD pipeline |
| [ADR Index](./adr/README.md) | All Architecture Decision Records |

---

## Technology Stack

| Layer | Technology | Why |
|---|---|---|
| Frontend | React 18 + Vite | Component model maps cleanly to the bill/item/person domain |
| Backend | ASP.NET Core Web API (.NET 8) | Typed domain model, DI built-in, strong C# interview requirement |
| Testing | xUnit + Moq + Coverlet | NatWest JD requirement; AAA pattern enforced by xUnit design |
| Infrastructure | Azure App Service + Static Web Apps | PaaS — no VM management, scales to zero, free tier available |
| IaC | Terraform (azurerm provider) | Reproducible infra, interview requirement, version-controlled state |

---

## Quick Start

```bash
# Frontend
cd splitDesk/frontend
npm install
npm run dev          # → http://localhost:5173

# Backend (once scaffolded)
cd splitDesk/backend/SplitDesk.Api
dotnet run           # → http://localhost:5000

# Terraform (once scaffolded)
cd splitDesk/infra
terraform init
terraform plan
terraform apply
```

---

## Project Status

- [x] Frontend scaffolded (React + Vite)
- [x] Engineering spec written
- [x] ADRs written
- [x] Architecture diagrams written
- [ ] Backend scaffolded (C# Web API)
- [ ] Unit tests written (xUnit + Moq)
- [ ] Terraform infra written
- [ ] E2E deployed to Azure
