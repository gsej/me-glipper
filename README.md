# me-glipper

A personal GLP-1 dose tracking app with an Angular frontend and a .NET backend API.
Records GLP-1 doses (when taken, medication, dose in mg, notes) per user. Forked from
a weight-tracking app (`me-tracker`); the weight → dose rename is in progress — see `TODO`.

## Architecture

| Component | Technology | Location |
|-----------|-----------|----------|
| Frontend | Angular (PWA) | `code/angular/` |
| Backend API | .NET 10 (ASP.NET Core) | `code/dotnet/Api/` |
| Database | SQLite | mounted from a private directory outside the repo |

The frontend is a static Angular app hosted on GitHub Pages. It communicates with a backend API running on a home server.

The backend runs in a Docker container. It reads and writes a SQLite database file that lives outside the repo in a private directory, which also holds secrets via a `.env` file.

## Deployment

### Frontend

Deployed via a GitHub Actions workflow (`.github/workflows/webui-prod.yml`), triggered with **workflow_dispatch** from the GitHub Actions UI.

The workflow builds the Angular app and deploys the static files to **GitHub Pages**. The production API URL is injected at build time via the `API_URL` environment variable into `settings.json` (which points to `localhost:5200` in source control for local development).

### Backend

Deployed to a **Proxmox** LXC as Infrastructure-as-Code:

- **`infra/`** — Terraform builds an unprivileged Ubuntu 24.04 LXC on the Proxmox
  host (M900), with a dedicated persistent volume for the SQLite DB. See
  `infra/README.md`.
- **`ansible/`** — Ansible installs Docker, clones this repo, builds/starts the
  API from `docker-compose.yml`, and exposes it over public HTTPS via Tailscale
  Funnel at `https://me-glipper-m900.<tailnet>.ts.net`. See `ansible/README.md`.

`docker-compose.yml` builds the image from `code/dotnet/Api/Dockerfile` and runs
it on port 5200, mounting the DB volume and `.env` from `/opt/me-glipper-data`
(provisioned by Terraform/Ansible). Because it uses those absolute paths, it is a
**deployment** compose — for local dev run the API directly with
`dotnet run --project code/dotnet/Api` instead.

#### Verifying the running build

The health endpoint returns the git hash the image was built from:

```bash
curl https://me-glipper-m900.<tailnet>.ts.net/api/healthz
# {"status":"Healthy","gitHash":"abc1234"}
```

Compare against `git rev-parse --short HEAD` to confirm the new image is serving.
