# Architecture Overview — splitDesk

> Render these diagrams in VS Code with the **Markdown Preview Mermaid Support** extension, or view on GitHub which renders Mermaid natively.

---

## C4 Level 1 — System Context

```mermaid
C4Context
  title splitDesk — System Context

  Person(user, "User", "Someone splitting a bill with friends")

  System(splitDesk, "splitDesk", "Web app that calculates fair bill splits based on item-level consumption")

  System_Ext(azure, "Microsoft Azure", "Cloud platform hosting the app services")
  System_Ext(browser, "Web Browser", "Chrome / Edge / Firefox")

  Rel(user, browser, "Uses")
  Rel(browser, splitDesk, "Sends bill data, receives split result", "HTTPS/JSON")
  Rel(splitDesk, azure, "Deployed on", "Terraform / ARM")
```

---

## C4 Level 2 — Container Diagram

```mermaid
C4Container
  title splitDesk — Containers

  Person(user, "User", "Splitting a bill")

  Container(spa, "React SPA", "React 18 + Vite", "Single-page app. Collects bill data, displays split result.")
  Container(api, "C# Web API", "ASP.NET Core .NET 8", "Stateless REST API. Runs split algorithm, validates input.")
  ContainerDb(mem, "In-Memory Store", "Dictionary<Guid, Bill>", "Ephemeral bill storage. No persistence between restarts (v1).")

  Rel(user, spa, "Interacts with", "Browser")
  Rel(spa, api, "POST /api/bills/split", "HTTPS JSON")
  Rel(api, mem, "Read/Write", "In-process")
```

---

## C4 Level 3 — Component Diagram (Backend)

```mermaid
C4Component
  title splitDesk Backend — Components

  Container_Boundary(api, "C# Web API") {
    Component(ctrl, "BillController", "ASP.NET Controller", "Routes HTTP requests, validates input, returns responses")
    Component(svc, "BillService", "C# Service", "Core split algorithm and business rules")
    Component(repo, "IBillRepository", "Interface", "Abstracts data access — enables Moq in tests")
    Component(impl, "InMemoryBillRepository", "Implementation", "Dictionary-backed store, no external dependency")
    Component(validator, "BillRequestValidator", "FluentValidation", "Validates request shape and business rules")
  }

  Rel(ctrl, validator, "Validates request")
  Rel(ctrl, svc, "Calls CalculateSplit()")
  Rel(svc, repo, "Calls via interface")
  Rel(repo, impl, "Resolved at runtime by DI")
```

---

## C4 Level 3 — Component Diagram (Frontend)

```mermaid
C4Component
  title splitDesk Frontend — Components

  Container_Boundary(spa, "React SPA") {
    Component(app, "App.jsx", "React Component", "Root. Owns loading/error/result state. Calls API.")
    Component(form, "BillForm.jsx", "React Component", "Owns bill metadata, people list, items list state")
    Component(people, "PeopleInput.jsx", "React Component", "Add/remove people. Lifts changes up to BillForm.")
    Component(items, "ItemInput.jsx", "React Component", "Add items with consumer assignment. Lifts up to BillForm.")
    Component(result, "SplitResult.jsx", "React Component", "Pure display. Renders API response. No state.")
    Component(api, "billApi.js", "Service Module", "Encapsulates fetch() call to backend. Single responsibility.")
  }

  Rel(app, form, "onSubmit prop + loading prop")
  Rel(app, result, "result prop")
  Rel(app, api, "calls calculateSplit()")
  Rel(form, people, "people + onAdd + onRemove props")
  Rel(form, items, "people + onAdd props")
```

---

## Technology Decision Map

```mermaid
graph TD
  subgraph Frontend
    A[React 18] -->|"component model"| B[JSX + Hooks]
    C[Vite] -->|"build tool"| A
    D[CSS Modules / plain CSS] -->|"styling"| A
  end

  subgraph Backend
    E[ASP.NET Core .NET 8] -->|"Web API"| F[Controllers]
    F --> G[Services]
    G --> H[Repositories]
    I[xUnit] -->|"test framework"| G
    J[Moq] -->|"mocking"| I
    K[Coverlet] -->|"coverage"| I
  end

  subgraph Infrastructure
    L[Terraform] -->|"provisions"| M[Azure Resource Group]
    M --> N[App Service Plan]
    N --> O[Linux Web App - API]
    N --> P[Static Web App - React]
    M --> Q[Azure Storage - Terraform state]
  end

  A -->|"HTTP POST"| E
```
