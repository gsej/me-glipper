# Frontend image for the Proxmox LXC: builds the Angular app and serves it with nginx.
#
# Served on the same origin as the API (home-proxy routes /api/* to the API and
# everything else here), so API_URL is baked in as '' and the app calls /api/...
# relative to wherever it was loaded from.
FROM node:22 AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
ARG GIT_HASH=unknown
RUN npm run build -- --base-href / \
      --define "GIT_HASH='${GIT_HASH}'" \
      --define "API_URL=''"

FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/me-glipper/browser /usr/share/nginx/html
