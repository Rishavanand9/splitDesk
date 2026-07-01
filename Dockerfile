# Single-container build for platforms that auto-deploy a root Dockerfile
# (Railway, Render, Fly.io, Cloud Run, etc.) — no nginx, no docker-compose.
# The API serves the built React app directly (see Program.cs's
# UseStaticFiles/MapFallbackToFile) as well as /api/*, both on one port.
#
# For local development, use `docker compose up --build` instead — it runs
# the frontend behind its own nginx container with hot-swappable config,
# which is nicer to iterate against than rebuilding this image every change.

# ── Stage 1: Build the React frontend ──────────────────────────────────────
FROM node:20-alpine AS frontend-build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/. .
# Empty string = relative URLs (/api/...) — same-origin now that the API
# itself serves this bundle, so no absolute URL needs to be baked in.
ENV VITE_API_URL=""
RUN npm run build

# ── Stage 2: Build the .NET API ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src
COPY backend/SplitDesk.Api/SplitDesk.Api.csproj SplitDesk.Api/
RUN dotnet restore SplitDesk.Api/SplitDesk.Api.csproj
COPY backend/. .
RUN dotnet publish SplitDesk.Api/SplitDesk.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 3: Runtime — API + built frontend in one image ──────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Tesseract OCR for the bill scan feature; curl for the platform healthcheck.
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        tesseract-ocr \
        tesseract-ocr-eng \
        curl && \
    rm -rf /var/lib/apt/lists/*

ENV TESSDATA_PREFIX=/usr/share/tesseract-ocr/5/tessdata

COPY --from=backend-build /app/publish .
COPY --from=frontend-build /app/dist ./wwwroot

# Most "auto-detect a Dockerfile" platforms inject $PORT at container start
# and expect the app to bind to it; default to 8080 when it's not set (e.g.
# Fly.io, where the port is pinned explicitly via fly.toml instead).
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["/bin/sh", "-c", "ASPNETCORE_URLS=http://+:${PORT} dotnet SplitDesk.Api.dll"]
