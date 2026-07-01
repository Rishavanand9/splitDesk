# ADR-001 — Use React + Vite for the Frontend

**Date:** 2026-07-01  
**Status:** Accepted

---

## Context

splitDesk needs a frontend framework. The NatWest JD explicitly lists React experience as a requirement. The application is a form-heavy SPA with a clear component hierarchy (bill → items → people), which maps naturally to React's component model. We need a modern build tool that starts fast and has good ecosystem support.

Options considered:
- **React + Vite** — current industry standard, fast HMR, native ESM
- **React + Create React App (CRA)** — outdated, no longer maintained by Meta
- **Vue 3** — excellent framework but not on the JD requirement list
- **Plain HTML/JS** — no framework, but loses component reusability and state management

## Decision

Use **React 18** with **Vite** as the build tool.

## Consequences

**Positive:**
- Directly addresses the JD requirement ("React JS" listed explicitly)
- Vite's Hot Module Replacement (HMR) gives instant feedback during development
- React's component model maps cleanly to the bill/item/person domain — each natural entity in the problem becomes a component
- Hooks (`useState`, `useEffect`) give functional components full state capabilities without class complexity
- Large ecosystem — easy to find documentation and extend later

**Negative:**
- React's unidirectional data flow requires thinking in terms of "lifting state up," which is non-obvious initially
- Vite config needs adjustment to proxy API calls in development (or use CORS + env var, which we chose)
- Bundle size is larger than a plain JS app (mitigated by Vite's tree-shaking)

## Interview Talking Point

> "I chose React because the job spec required it, but also because the bill/people/items domain has a clear component hierarchy that React's composition model handles well. I used Vite instead of CRA because CRA is deprecated — Vite is the current recommended tool from the React team."
