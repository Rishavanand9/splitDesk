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
# Custom nginx config to handle React Router (SPA routing)
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
