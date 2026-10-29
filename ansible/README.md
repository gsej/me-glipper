# me-glipper deployment (Ansible)

Terraform (`../infra`) builds the LXC; this playbook configures it: installs
Docker, clones the repo, and builds/starts the API from
`docker-compose.yml`.

## Structure

```
ansible/
├── ansible.cfg              # connects as root; no default inventory (pass -i)
├── m900-inventory.ini       # M900 container (.222, ts node me-glipper-m900)
├── requirements.yml        # community.docker collection
├── secrets/                # control-machine secrets (gitignored) + sample templates
├── site.yml                # top-level playbook → runs the meglipper role
└── roles/meglipper/
    ├── defaults/main.yml   # repo URL, paths, .env + Tailscale secret sources
    ├── handlers/main.yml   # restart tailscaled
    └── tasks/
        ├── main.yml        # install Docker, clone, deliver .env, build + up
        └── tailscale.yml   # install Tailscale, authenticate, expose via Funnel
```

## One-time setup

Arch's `ansible` package bundles `community.docker`, so no galaxy step:

```sh
sudo pacman -Syu ansible
```

## Run it

The deployment target is M900, with its committed inventory file
(`m900-inventory.ini`) — the non-secret parallel to the per-env `tfvars`. No
inventory is auto-loaded, so pass it with `-i`.

```sh
cd ansible
ansible -i m900-inventory.ini meglipper -m ping     # connectivity check
ansible-playbook -i m900-inventory.ini site.yml
```

The playbook:
1. installs Docker,
2. clones the public `me-glipper` repo to `/opt/me-glipper`,
3. copies `.env` from `secrets/.env` to `/opt/me-glipper-data/.env` (the DB is
   **not** copied — the app creates an empty one; restore real data via the API
   out-of-band),
4. builds and starts the API, then waits for `/api/healthz`,
5. installs Tailscale and exposes the API over public HTTPS via Funnel
   (phase 2 — see below).

- LAN (phase 1): `http://<container_ip>:5200`.
- Public (phase 2): `https://<hostname>.<tailnet>.ts.net` — the exact URL is
  printed by the final playbook task (`tailscale funnel status`).

## Data & restore

The SQLite DB lives on a dedicated Terraform mount at
`/opt/me-glipper-data/database`, so it survives container rebuilds. After first
deploy it's empty — restore a recent backup via the API restore endpoint.

## Public exposure (phase 2 — Tailscale Funnel)

The API is exposed to the internet over HTTPS via Tailscale Funnel, configured
by `tasks/tailscale.yml`. The GitHub Pages frontend calls this public URL.

Because the LXC is **unprivileged** (no `/dev/net/tun`), `tailscaled` runs in
**userspace-networking** mode — Funnel only proxies a local TCP port, which that
mode supports without any device passthrough. Nothing in Terraform changes.

### One-time Tailscale admin-console setup

None of this lives in source control (it's tailnet state), so redo it on a fresh
tailnet. In the [admin console](https://login.tailscale.com/admin):

1. **Policy** (Access Controls):
   - `tagOwners`: `"tag:meglipper": ["autogroup:admin"]`.
   - `nodeAttrs`: grant the `funnel` attribute to `tag:meglipper`. The default
     funnel entry targets `autogroup:member`, which does **not** include tagged
     devices — so add `tag:meglipper` explicitly.
2. **HTTPS** (DNS settings): enable MagicDNS + HTTPS certificates (Funnel needs it).
3. **OAuth client** (Settings → Trust credentials → Generate credential → OAuth):
   scope `auth_keys` → **Write** only (not "All"), tag `tag:meglipper`. Copy the
   one-time secret.

### Secrets

Deploy secrets live in `ansible/secrets/` on the control machine. The real files
are **gitignored** (never committed to this public repo); the sample templates
and the **public** key are tracked. Restore/copy each from your password manager:

- `env.sample` → `secrets/.env` — API keys (`ApiKeys=...`), delivered to the box
  as `/opt/me-glipper-data/.env`.
- `tailscale-oauth.env.example` → `secrets/tailscale-oauth.env` — the Tailscale
  OAuth client secret, one line:
  ```sh
  TS_OAUTH_SECRET=tskey-client-xxxxxxxx
  ```
- `secrets/deploy_key` (+ committed `secrets/deploy_key.pub`) — the SSH deploy
  key. Terraform injects the public key into the container; Ansible connects with
  the private key. Restore the private key from your password manager and
  `chmod 600` it.

The `.env`/OAuth source paths are `meglipper_env_src` / `meglipper_ts_oauth_src`
in `defaults/main.yml`; the key is referenced by `<env>-inventory.ini` (public)
and `infra/*-terraform.tfvars` (public). The playbook checks `.env` and the OAuth
secret exist up front and fails with a clear message if one is missing.

### Notes

- Re-runs are idempotent: `tailscale up` is skipped once the node is `Running`,
  and Funnel is only (re)set if port 5200 isn't already published.
- Unlike the app code, Tailscale config takes effect **without** pushing to
  `origin/master` — it runs from this control-machine checkout, not the box's
  clone. (The push rule only applies to app code that Ansible builds on the box.)
- `terraform destroy`/recreate leaves a stale node in the tailnet; delete it in
  the console or re-run the playbook to re-adopt the hostname.
