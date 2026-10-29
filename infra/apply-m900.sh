#!/usr/bin/env bash
# Terraform apply for the M900 environment: selects the `m900` workspace (separate
# state under terraform.tfstate.d/m900/) and loads the M900 var files.
# Tear down with: ./apply-m900.sh -destroy
set -euo pipefail
cd "$(dirname "$0")"
terraform workspace select m900 >/dev/null 2>&1 || terraform workspace new m900
terraform apply \
  -var-file=m900-terraform.tfvars \
  -var-file=m900-secrets.tfvars \
  "$@"
