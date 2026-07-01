# Deploying via GitHub Actions (Fly.io)

The workflow at `.github/workflows/deploy.yml` runs backend tests on every push/PR, and — on pushes to `master` — builds and deploys both containers to [Fly.io](https://fly.io). Fly was chosen because it deploys the existing `Dockerfile`s as-is, including Tesseract OCR for the backend (a plain zip/code deploy wouldn't have Tesseract installed).

This requires a few one-time manual steps that only you can do (they need your Fly.io account):

## 1. Install flyctl and log in

```bash
curl -L https://fly.io/install.sh | sh
fly auth login
```

## 2. Create the two Fly apps

App names are globally unique across all of Fly — `splitdesk-api` and `splitdesk-web` (used in `backend/fly.toml` and `frontend/fly.toml`) may already be taken. If so, pick different names and update:
- `app = "..."` in both `fly.toml` files
- `API_UPSTREAM` in `frontend/fly.toml` to match the API app's new name

```bash
cd backend  && fly apps create splitdesk-api
cd ../frontend && fly apps create splitdesk-web
```

## 3. Generate a deploy token and add it to GitHub

```bash
fly tokens create deploy -x 999999h
```

Copy the output, then in the GitHub repo: **Settings → Secrets and variables → Actions → New repository secret**, name it `FLY_API_TOKEN`, and paste the token.

## 4. Push to `master`

That's it — the workflow tests, builds, and deploys both apps automatically. Check progress under the repo's **Actions** tab.

- Frontend: `https://splitdesk-web.fly.dev`
- API: `https://splitdesk-api.fly.dev`

## Notes

- Both apps use `auto_stop_machines`/`min_machines_running = 0` (scale-to-zero) to stay within a small free/low-cost allowance — the first request after idling will be a few seconds slower while the machine cold-starts.
- Fly's pricing/free allowance changes over time — check [fly.io/pricing](https://fly.io/pricing) before deploying if cost matters. Render is a comparable alternative if you'd rather use that instead; the same Dockerfiles work there too, just swap the deploy steps in the workflow for Render's GitHub Action.
- The Azure Terraform under `infra/` is a separate, more involved path (see [architecture/deployment.md](architecture/deployment.md)) — not used by this workflow.
