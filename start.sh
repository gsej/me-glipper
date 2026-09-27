#!/usr/bin/env bash
#
# Start the backend API container, building the image first.
#
# Injects the current git short hash into the image at build time and forces a
# rebuild, so the running container always reflects the checked-out source. Run
# `git pull` first to pick up the latest source.
#
# Verify what is serving afterwards:
#   curl http://localhost:5200/api/healthz
set -euo pipefail

cd "$(dirname "$0")"

GIT_HASH="$(git rev-parse --short HEAD)"
export GIT_HASH

echo "Building and starting API at git hash ${GIT_HASH}..."
docker compose up -d --build

echo "Done. Serving git hash ${GIT_HASH}."
