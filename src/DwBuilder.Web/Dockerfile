# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: Build React/Vite app
# ──────────────────────────────────────────────────────────────────────────────
FROM node:22-alpine AS build
WORKDIR /app

# VITE_API_URL viene iniettato a build-time (Vite lo embeds nel bundle)
ARG VITE_API_URL=http://localhost:5000
ENV VITE_API_URL=$VITE_API_URL

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: Serve with nginx
# ──────────────────────────────────────────────────────────────────────────────
FROM nginx:alpine AS runtime

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
