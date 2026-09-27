# me-glipper

A personal weight tracking app with an Angular frontend and a .NET backend API.

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

Deployed on the home server:

```bash
git pull
./start.sh
```

- **`start.sh`** — builds the image and starts the container. Use for a cold start.
- **`restart.sh`** — stops the running container, then rebuilds and restarts it.

`docker-compose.yml` builds the image from `code/dotnet/Api/Dockerfile` and starts the container on port 5200, with the database volume and secrets mounted from the private directory.

#### Verifying the running build

The health endpoint returns the git hash the image was built from:

```bash
curl https://<server>/api/healthz
# {"status":"Healthy","gitHash":"abc1234"}
```

Compare against `git rev-parse --short HEAD` to confirm the new image is serving.
