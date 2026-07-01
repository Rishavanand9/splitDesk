# Multi-stage build — stage 1 compiles, stage 2 runs.
# The runtime image also installs Tesseract OCR for the bill scan feature.

# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SplitDesk.Api/SplitDesk.Api.csproj SplitDesk.Api/
RUN dotnet restore SplitDesk.Api/SplitDesk.Api.csproj

COPY . .
RUN dotnet publish SplitDesk.Api/SplitDesk.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install Tesseract OCR and English language data.
# curl is also installed so the Docker healthcheck can ping /health.
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        tesseract-ocr \
        tesseract-ocr-eng \
        curl && \
    rm -rf /var/lib/apt/lists/*

# Tell Tesseract where its language data lives (varies by distro/version)
ENV TESSDATA_PREFIX=/usr/share/tesseract-ocr/5/tessdata

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8082
EXPOSE 8082

ENTRYPOINT ["dotnet", "SplitDesk.Api.dll"]
