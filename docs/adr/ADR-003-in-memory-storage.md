# ADR-003 — Use In-Memory Repository for v1 (No Database)

**Date:** 2026-07-01  
**Status:** Accepted

---

## Context

splitDesk needs to store bill data temporarily during a calculation request. The question is whether to use a real database (Azure SQL, Cosmos DB, SQLite) or an in-memory store for v1.

The primary use case is **stateless calculation** — users enter a bill, get a result, and leave. There is no requirement to:
- Save bills for later retrieval
- Share bills via a link
- Authenticate users and show their bill history

This is a time-constrained project (built alongside interview prep). Introducing a real database adds: connection string management, migration scripts, integration test complexity, and Azure resource cost.

## Decision

Use an **in-memory Dictionary-backed repository** (`InMemoryBillRepository`) implementing `IBillRepository` for v1.

The repository interface is defined properly so that swapping to a real database in v2 requires only:
1. Adding a new `SqlBillRepository : IBillRepository` class
2. Changing one DI registration line in `Program.cs`
3. No changes to `BillService` or `BillController`

## Consequences

**Positive:**
- Zero infrastructure dependency — runs locally with `dotnet run`, no connection strings, no Docker
- Enables `terraform destroy` between sessions without losing functionality (nothing is persisted)
- `BillService` tests mock `IBillRepository` — no integration test complexity for v1
- Demonstrates the Repository pattern and Dependency Inversion correctly — the abstraction is real and meaningful, not just theoretical
- Faster to build and deploy (one fewer Azure resource)

**Negative:**
- Bill data is lost on app restart — acceptable for v1 (stateless calculator)
- Cannot demonstrate SQL skills via this project (covered separately in interview prep)
- `InMemoryBillRepository` is effectively untestable in a meaningful way (it's just a dictionary) — but this is fine since the service layer is what we test

## If This Were Production

We would use **Azure SQL Database** with Entity Framework Core:
- `BillRepository : IBillRepository` using `DbContext`
- Migrations via `dotnet ef migrations add`
- Connection string from Azure Key Vault, not environment variables
- Integration tests using `Testcontainers` (spins up a real SQL container per test run)

## Interview Talking Point

> "I used in-memory storage for v1 because the core requirement is stateless calculation — no user ever asked to retrieve a past bill. More importantly, I defined the repository as an interface from day one, so swapping to Azure SQL in v2 is a single-line DI change. That's Dependency Inversion working as intended — the business logic doesn't care where the data lives."
