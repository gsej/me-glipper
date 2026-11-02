#!/usr/bin/env bash
# Redeploy the me-glipper API to the M900 Proxmox LXC.
#
# Re-runs the Ansible playbook, which updates the box's clone to origin/master,
# rebuilds the image, restarts the container, and waits for /api/healthz.
# Idempotent. The SQLite DB is on a separate mount and is not touched.
#
# Prerequisite: the API code must already be pushed to origin/master — the box
# builds from its own clone, not this checkout.
set -euo pipefail

cd "$(dirname "$0")/ansible"
ansible-playbook -i m900-inventory.ini site.yml
