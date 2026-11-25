#!/usr/bin/env bash
# Redeploy me-glipper (the API and the frontend) to the M900 Proxmox LXC.
#
# Re-runs the Ansible playbook, which updates the box's clone to origin/master,
# rebuilds both images, restarts the containers, and waits for /api/healthz
# (port 5200) and the frontend (port 5201).
# Idempotent. The SQLite DB is on a separate mount and is not touched.
#
# Prerequisite: the code must already be pushed to origin/master — the box
# builds from its own clone, not this checkout.
set -euo pipefail

cd "$(dirname "$0")/ansible"
ansible-playbook -i m900-inventory.ini site.yml
