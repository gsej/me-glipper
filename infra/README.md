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
  `/opt/me-glipper-data/database`, separate from the root filesystem. Terraform owns this volume, so
  destroying or replacing the LXC **destroys the database**: back it up first.

## Prerequisites

1. A Proxmox node reachable over HTTPS. The Ubuntu template is downloaded by
   Terraform (adjust `template_url` if the pinned version 404s).
2. The Proxmox `root@pam` password, exported as `TF_VAR_proxmox_password` in
   `~/.config/homelab/secrets.env` (outside the repo, mode 600; master copy in your
   password manager). The wrappers load it.
3. A deploy SSH keypair at `../ansible/secrets/deploy_key{,.pub}`. The public key
   is committed and injected into the container; the private key is gitignored —
   restore it from your password manager (`chmod 600`), or generate a fresh pair:

   ```sh
   ssh-keygen -t ed25519 -f ../ansible/secrets/deploy_key -C "me-glipper-lxc" -N ""
   ```
   (If you regenerate, commit the new `deploy_key.pub`.)

## Root access

Containers have no usable root password. Use the deploy key over SSH, or run
`pct enter <vmid>` from the Proxmox host's shell. The Console tab in the Proxmox
web UI will show a login prompt that can't be used.

## Environments, variables & state

The deployment target is the **M900** Proxmox host. Config is split so secrets
never get committed:

- `m900-terraform.tfvars` — **committed**, non-secret config (endpoint, node,
  networking, hostname, …).
- Each environment uses its own **Terraform workspace**, so state is isolated
  under `terraform.tfstate.d/<env>/`. This box uses the `m900` workspace (the
  wrapper scripts select it automatically).

The var file isn't auto-loaded (it isn't named `terraform.tfvars`), so it's passed
explicitly. The wrapper scripts do that *and* select the workspace:

```sh
cd infra
terraform init
./plan-m900.sh
./apply-m900.sh
terraform output                                # note the container IP (192.168.1.222)
# tear down:  ./apply-m900.sh -destroy
```

Then deploy the API: see `../ansible/README.md`.

### Adding another environment (later)

Copy `m900-terraform.tfvars` → `<env>-terraform.tfvars` and edit the endpoint,
node, IP, and hostname.
Add `plan-<env>.sh` / `apply-<env>.sh` (the `m900` scripts with `<env>` swapped
in) — they run in a separate workspace, so state is isolated per environment.
