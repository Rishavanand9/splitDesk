# ADR-002 — Use ASP.NET Core Web API for the Backend

**Date:** 2026-07-01  
**Status:** Accepted

---

## Context

splitDesk needs a backend API to run the split algorithm. The NatWest JD explicitly requires "Strong experience in C# development" and lists ASP.NET Core implicitly via Azure Functions, Web Apps, and .NET patterns (SOLID, Clean Code, design patterns). The backend needs to be testable, maintainable, and deployable to Azure App Service.

Options considered:
- **ASP.NET Core Web API (.NET 8)** — C# native, DI built-in, strong typing, Azure-native
- **Azure Functions (C#)** — serverless, also on the JD, but stateless HTTP triggers are almost identical to a controller action and have more cold-start complexity
- **Node.js (Express)** — would duplicate the React layer, no C# interview benefit
- **Python (FastAPI)** — not on JD, no interview value for NatWest

## Decision

Use **ASP.NET Core Web API with .NET 8** hosted as an Azure Linux Web App.

## Consequences

**Positive:**
- Directly matches the C# requirement on the JD
- Built-in dependency injection container — no third-party DI framework needed
- Controller → Service → Repository layering is idiomatic C# and directly demonstrable in interview
- Strong typing catches bugs at compile time that dynamic languages would miss at runtime
- `ILogger<T>` structured logging is built in — no extra package for observability
- Swagger/OpenAPI generated automatically in development — useful for API exploration

**Negative:**
- More boilerplate than a Function App for a simple stateless endpoint
- Requires an App Service Plan (costs ~£10/month) vs Consumption-plan Functions (costs near zero for low traffic)
- .NET startup time is non-trivial for containers (mitigated by App Service warm instances)

## Note on Azure Functions

Azure Functions (HTTP trigger) was a viable alternative and is explicitly on the JD. The reason we chose Web API over Functions is that the Controller → Service → Repository pattern is easier to demonstrate unit testing with xUnit + Moq — a cleaner interview story. The architectural patterns (DI, SOLID, layering) are identical.

## Interview Talking Point

> "I used ASP.NET Core Web API because C# is a core requirement for the role. I structured it with Controller → Service → Repository layers to demonstrate SOLID principles — specifically Dependency Inversion, since the Service only knows about the repository interface, never the implementation. This makes the service trivially testable with Moq."
