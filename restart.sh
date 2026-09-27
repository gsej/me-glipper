#!/usr/bin/env bash
#
# Restart the backend API container, rebuilding the image first.
#
# Stops and removes the running container, then rebuilds (injecting the current
# git short hash) and starts it again via start.sh. Run `git pull` first to pick
# up the latest source.
set -euo pipefail

cd "$(dirname "$0")"

echo "Stopping API container..."
docker compose down

exec ./start.sh
