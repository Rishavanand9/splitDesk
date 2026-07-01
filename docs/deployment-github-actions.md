# Deploying via GitHub Actions (Fly.io)

The root `Dockerfile` builds a single container: the ASP.NET Core API also serves the built React app directly (no nginx) — see `Program.cs`'s `UseStaticFiles`/`MapFallbackToFile`. This is what `.github/workflows/deploy.yml` tests, builds, and deploys, and it's also what platforms like Railway, Render, or Fly.io auto-detect when you point them at this repo (a `Dockerfile` at the root, one service).

The workflow runs backend tests on every push/PR, and — on pushes to `master` — builds the root Dockerfile and deploys it to [Fly.io](https://fly.io) via `fly.toml` at the repo root.

This requires a few one-time manual steps that only you can do (they need your Fly.io account):

## 1. Install flyctl and log in

```bash
curl -L https://fly.io/install.sh | sh
fly auth login
```

## 2. Create the Fly app

`splitdesk` (set in `fly.toml`) may already be taken — Fly app names are globally unique. If so, pick a different name and update `app = "..."` in `fly.toml`.

```bash
fly apps create splitdesk
```

## 3. Generate a deploy token and add it to GitHub

```bash
fly tokens create deploy -x 999999h
```

Copy the output, then in the GitHub repo: **Settings → Secrets and variables → Actions → New repository secret**, name it `FLY_API_TOKEN`, and paste the token.

## 4. Push to `master`

That's it — the workflow tests, builds, and deploys automatically. Check progress under the repo's **Actions** tab.

- App: `https://splitdesk.fly.dev` (frontend and API both served from here — `/api/*` for the API, everything else is the SPA)

## Running the same image locally

```bash
docker build -t splitdesk .
docker run -p 8080:8080 splitdesk
```

For day-to-day local development, `docker compose up --build` (two containers, nginx + API) is still the better loop — it's what's documented in the root README. This root Dockerfile exists specifically for single-container deployment.

## Notes

- The app uses `auto_stop_machines`/`min_machines_running = 0` (scale-to-zero) to stay within a small free/low-cost allowance — the first request after idling will be a few seconds slower while the machine cold-starts.
- Fly's pricing/free allowance changes over time — check [fly.io/pricing](https://fly.io/pricing) before deploying if cost matters. Render, Railway, and Cloud Run are comparable alternatives that also auto-detect a root Dockerfile — swap the `deploy` job's steps in the workflow for that platform's GitHub Action/CLI if you'd rather use one of those.
- The Azure Terraform under `infra/` is a separate, more involved path (see [architecture/deployment.md](architecture/deployment.md)) — not used by this workflow, and provisions a zip-deployed Web App that wouldn't have Tesseract installed anyway.
