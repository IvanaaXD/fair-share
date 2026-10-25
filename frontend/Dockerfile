# FairShare web app - container image: the React app is built here and served by Nginx.
# Build context: the frontend folder. The Nginx configuration and the HTTPS certificate are
# mounted by docker-compose.yml (nginx/nginx.conf, nginx/certs), so they are not baked in.

# ---------- build stage: Node.js, used only to build the static files ----------
FROM node:22-alpine AS build
WORKDIR /app

# Dependencies first, so "npm ci" is cached and only re-runs when package-lock.json changes.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---------- runtime stage: only Nginx and the built files ----------
FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80 443
