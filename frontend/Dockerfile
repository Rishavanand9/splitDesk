# Multi-stage: stage 1 builds the React app, stage 2 serves it with nginx.

# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files first — Docker caches npm install if package.json unchanged
COPY package*.json ./
RUN npm ci

# Copy source and build
COPY . .
# Empty string = relative URLs (/api/...) — nginx proxies them to the API container.
# No URL is baked into the bundle, so the same image works in dev and prod.
ENV VITE_API_URL=""
RUN npm run build
# Output lands in /app/dist

# ── Stage 2: Serve ───────────────────────────────────────────────────────────
FROM nginx:alpine AS runtime
# Copy the built React app into nginx's web root
COPY --from=build /app/dist /usr/share/nginx/html
# Templated nginx config — nginx:alpine auto-renders /etc/nginx/templates/*.template
# via envsubst on startup. Defaults below match Docker Compose's internal network;
# override API_UPSTREAM/DNS_RESOLVER at deploy time when the API is a separate host.
COPY nginx.conf.template /etc/nginx/templates/default.conf.template
ENV API_UPSTREAM=http://api:8082
ENV DNS_RESOLVER=127.0.0.11
ENV NGINX_ENVSUBST_FILTER=API_UPSTREAM|DNS_RESOLVER
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
