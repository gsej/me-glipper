# me-glipper

A personal GLP-1 dose tracking app with an Angular frontend and a .NET backend API.
It records GLP-1 doses (when taken, medication, dose in mg, notes) per user, estimates how
much of each medication remains in the body (charted, with a projection assuming no further
doses), and shows when the next dose is due. Forked from a weight-tracking app (`me-tracker`).

The app has three pages, swiped between:

- **Overview** — the next dose due for each medication being taken, then a chart per
  medication with its current estimated amount. Tapping a next-dose card opens it on the
  Medication page.
- **Medication** — one medication, chosen from a dropdown: its next-dose details, its chart,
  and a form to add a dose of it.
- **Dose History** — every dose recorded. Each dose's notes can be edited, and a dose can be
  deleted.

Ongoing work and known issues are tracked in `TODO`.

## Architecture

| Component | Technology | Location |
|-----------|-----------|----------|
| Frontend | Angular (PWA) | `code/angular/` |
| Backend API | .NET 10 (ASP.NET Core) | `code/dotnet/Api/` |
| Database | SQLite, schema managed by DbUp migrations | mounted from a private directory outside the repo |

Both run on a home server, each in its own Docker container: the API, and nginx serving the built Angular app. A reverse proxy (the `home-proxy` repo) serves them together at `https://me-glipper.gsej.co.uk`, sending `/api/*` to the API and everything else to the frontend.

The API reads and writes a SQLite database file that lives outside the repo in a private directory, which also holds secrets via a `.env` file.

The schema is created and changed by numbered SQL migrations in `code/dotnet/Api/Migrations/`, which the API runs on startup (DbUp). The medications the app knows about, with their dosing intervals and pharmacokinetic values, are reference data seeded by those migrations; see `docs/medications.md`.

## Deployment

Both parts are deployed to a **Proxmox** LXC as Infrastructure-as-Code:

- **`infra/`** — Terraform builds an unprivileged Ubuntu 24.04 LXC on the Proxmox
  host (M900), with a dedicated persistent volume for the SQLite DB. See
  `infra/README.md`.
- **`ansible/`** — Ansible installs Docker, clones this repo, and builds/starts
  the API and the frontend from `docker-compose.yml`. See `ansible/README.md`.

To deploy, push to `master` (the box builds from its own clone), then run:

```bash
./deploy.sh
```

`docker-compose.yml` has two services:

- **`api`** builds from `code/dotnet/Api/Dockerfile` and runs on port 5200,
  mounting the DB volume and `.env` from `/opt/me-glipper-data` (provisioned by
  Terraform/Ansible).
- **`web`** builds the Angular app with `code/angular/Dockerfile` and serves it
  with nginx on port 5201. The build bakes in the commit hash and an empty API
  URL (`ng build --define API_URL=''`), so the app calls `/api/...` on whichever
  site served it. Without these defines, as in local development, the app uses
  `http://localhost:5200` and shows `local` as the UI version in the π settings
  modal.

The proxy in `home-proxy` is what makes them public; nothing on this box is
exposed to the internet directly.

Because `docker-compose.yml` uses those absolute paths, it is a
**deployment** compose — for local dev run the API directly with
`dotnet run --project code/dotnet/Api` instead.

Database migrations run when the API starts, so take a backup before deploying a change that
adds one. If a migration fails, its transaction rolls back and the API doesn't start, so the
deploy's health check fails and the data is left as it was.

### Verifying the running build

The health endpoint returns the git hash the image was built from:

```bash
curl https://me-glipper.gsej.co.uk/api/healthz
# {"status":"Healthy","gitHash":"abc1234"}
```

Compare against `git rev-parse --short HEAD` to confirm the new image is serving.
