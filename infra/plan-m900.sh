#!/usr/bin/env bash
# Terraform plan for the M900 environment: selects the `m900` workspace (separate
# state under terraform.tfstate.d/m900/) and loads the M900 var files.
# Extra args pass through, e.g. ./plan-m900.sh -destroy
set -euo pipefail
cd "$(dirname "$0")"
# The Proxmox root@pam password lives outside the repo; the master copy is in Dashlane.
SECRETS_FILE="$HOME/.config/homelab/secrets.env"
if [[ ! -r "$SECRETS_FILE" ]]; then
  echo "Missing $SECRETS_FILE (it must export TF_VAR_proxmox_password)" >&2
  exit 1
fi
source "$SECRETS_FILE"
terraform workspace select m900 >/dev/null 2>&1 || terraform workspace new m900
terraform plan \
  -var-file=m900-terraform.tfvars \
  -var-file=m900-secrets.tfvars \
  "$@"
