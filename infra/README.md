# me-glipper on Proxmox (Terraform)

Builds an Ubuntu 24.04 LXC on Proxmox for the me-glipper API. Terraform only
**creates the container**; installing Docker and deploying the API is done by
Ansible in `../ansible`. State is local, kept separate per environment via
Terraform workspaces (see below).

## Design

- **Unprivileged LXC** — the API is internet-facing (Tailscale Funnel, phase 2)
  and needs no device passthrough, so unprivileged gives stronger isolation.
  `nesting`+`keyctl` are still needed for Docker-in-LXC.
- **root@pam auth** — the provider authenticates with username/password, not an
  API token: tokens can't set feature flags like `keyctl`.
- **Self-contained template** — downloads its own Ubuntu template
  (`overwrite_unmanaged` adopts a copy another repo already fetched), so this
  repo doesn't depend on another one having run first.
- **Dedicated data mount** — the SQLite DB lives on its own volume at
  `/opt/me-glipper-data/database` so a container rebuild never destroys it.

## Prerequisites

1. A Proxmox node reachable over HTTPS. The Ubuntu template is downloaded by
   Terraform (adjust `template_url` if the pinned version 404s).
2. The Proxmox `root@pam` password (in `<env>-secrets.tfvars` or `TF_VAR_proxmox_password`).
3. A deploy SSH keypair at `../ansible/secrets/deploy_key{,.pub}`. The public key
   is committed and injected into the container; the private key is gitignored —
   restore it from your password manager (`chmod 600`), or generate a fresh pair:

   ```sh
   ssh-keygen -t ed25519 -f ../ansible/secrets/deploy_key -C "me-glipper-lxc" -N ""
   ```
   (If you regenerate, commit the new `deploy_key.pub`.)

## Environments, variables & state

The deployment target is the **M900** Proxmox host. Config is split so secrets
never get committed:

- `m900-terraform.tfvars` — **committed**, non-secret config (endpoint, node,
  networking, hostname, …).
- `m900-secrets.tfvars` — **gitignored** (`infra/*-secrets.tfvars`): just the
  passwords; keep them in your password manager. Copy `secrets.tfvars.example`.
- Each environment uses its own **Terraform workspace**, so state is isolated
  under `terraform.tfstate.d/<env>/`. This box uses the `m900` workspace (the
  wrapper scripts select it automatically).

Neither var file is auto-loaded (they aren't named `terraform.tfvars`), so they're
passed explicitly. The wrapper scripts do that *and* select the workspace:

```sh
cd infra
cp secrets.tfvars.example m900-secrets.tfvars   # then fill in from your password manager
terraform init
./plan-m900.sh
./apply-m900.sh
terraform output                                # note the container IP (192.168.1.222)
# tear down:  ./apply-m900.sh -destroy
```

Then deploy the API: see `../ansible/README.md`.

### Adding another environment (later)

Copy `m900-terraform.tfvars` → `<env>-terraform.tfvars` and edit the endpoint,
node, IP, and hostname. Copy `secrets.tfvars.example` → `<env>-secrets.tfvars`.
Add `plan-<env>.sh` / `apply-<env>.sh` (the `m900` scripts with `<env>` swapped
in) — they run in a separate workspace, so state is isolated per environment.
