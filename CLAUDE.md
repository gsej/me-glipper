# CLAUDE.md

Guidance for working in this repo. Architecture and deployment are documented in
`README.md` — this file focuses on how to build/test/run and the conventions and
gotchas that aren't obvious from the code.

## What this is

A GLP-1 dose tracking app, forked from a weight-tracking app (`me-tracker`). It
records doses per user (time taken, medication, amount in mg, notes), estimates
how much of each drug remains in the body (numbers and charts), and shows when the
next dose is due. GLP-1 medications may be injected or taken orally, so "dose" is
the preferred domain term — not "injection". The structure and tooling are
inherited from the original app — treat it as production software. Clean,
reusable patterns (test scaffolding, clear layering) are valued.

## Layout

- `code/dotnet/` — ASP.NET Core Web API (`Api/`) + xUnit tests (`Tests/`), `me-glipper.sln`.
- `code/angular/` — Angular PWA frontend.
- `code/tests/rest/` — `.http` files for hitting the API by hand.
- `code/tests/jest/` — Jest setup (frontend).

## Toolchain (current)

- **.NET 10** (`net10.0`) — both `Api` and `Tests`.
- **Angular 22**, **TypeScript ~5.9**. Uses the `@angular/build` application builder.
- **Node**: Angular 22 requires Node ≥ 22.22.3 (or ≥24.15.0 / ≥26). The frontend image builds with `node:22`.
- **`@angular/build` must track the Angular major** — after `ng update` it can get
  pinned to a newer major than core (e.g. 22 while on 19); fix it to match.

## Common commands

Backend (from `code/dotnet/`):
```bash
dotnet build
dotnet test                     # xUnit
dotnet run --project Api        # local API on http://localhost:5200
```

Frontend (from `code/angular/`):
```bash
npm start                       # ng serve on http://localhost:4200
npm run build                   # output to dist/me-glipper/browser
npx ng test --watch=false --browsers=ChromeHeadless
```

Local dev: the API's `appsettings.json` uses a local SQLite file and an API key
of `apikey` (user `ApiUser`). The Angular app's `apiUrl` and UI git hash are
build-time constants (`ng build --define API_URL=… --define GIT_HASH=…`, see
`src/build-constants.d.ts`). Without them, as in local dev, it uses
`http://localhost:5200` and `local`.
New app versions are announced by an update banner (`AppUpdateService`, via
`SwUpdate`), which also checks for updates when the app returns to the foreground.
The service worker is off in `ng serve`, so the banner never shows in local dev.

## Testing conventions

- **xUnit + AwesomeAssertions** (a FluentAssertions fork — do **not** add
  FluentAssertions) + **NSubstitute** for mocks.
- **Integration tests** use `Tests/Integration/ApiTestFactory.cs` — a
  `WebApplicationFactory<Program>` that runs the API in-process against a
  throwaway temp SQLite file, with test API keys for users `alice`/`bob`. Reuse
  it for any endpoint/auth/per-user-scoping test. `Program` is a non-static
  class specifically so it can be the factory's entry point.
- **Repository tests** (`Tests/DataAccess/`) run against a temp SQLite file per
  test class.
- Angular: the default CLI scaffold specs were removed. There are currently **no**
  Angular specs, so `ng test` errors with `TS18003` until a real spec is added.
  There's no CI: the frontend is only compiled when the box builds its Docker image.

## Domain / API notes

- **Medications are reference data** in the `Medications` table, seeded and changed only
  by migrations (not editable from the app, not in backups). Doses refer to them by
  `MedicationId` (a foreign key); the API and backup JSON use the medication's **name**,
  resolved server-side ignoring case, and an unknown name is a `400`. Foreign keys are
  enforced because every repository adds `Foreign Keys=True` to its connection string
  (`ConnectionStrings.WithForeignKeys`). Values, sources and how to change them:
  `docs/medications.md`.
- **API-key auth** via the `X-API-Key` header (`ApiKeyAuthFilter`); keys come from
  the `ApiKeys` config value. Every request is scoped to the key's `UserId`.
- **Per-user isolation** is a real invariant — repositories filter by `userId`;
  one user must never read another's data. Keep it covered by tests.
- **Doses are soft-deleted** (a `Deleted` flag); normal reads exclude them, the
  backup endpoint reads everything.
- **A dose carries optional free-text `Notes`** — nullable, capped at
  200 chars (via `[MaxLength]` on the request records; `[ApiController]` turns that
  into an automatic `400`), with empty/whitespace normalised to `NULL` in the
  repository. Set it on create, or edit it later via `PUT /api/dose/{id}`. Use the
  name **`Notes`** consistently (entity/column/request/API/service/UI) — never `Comment`.
- **Schema changes are DbUp migrations** — numbered SQL scripts in `Api/Migrations/`
  (`0002_AddSomething.sql`, …), embedded in the assembly and run in filename order on
  startup by `DataAccess.DatabaseMigrator` (also in the integration and repository
  tests). Applied scripts are recorded in the `SchemaVersions` table. **Never edit a
  script once it has been deployed** — add a new one. Startup fails if a script fails,
  so a bad migration fails the deploy's health check. SQLite's `ALTER TABLE` can only
  add, rename and drop columns; changing a column's type or constraints needs a table
  rebuild (create new table, copy rows, drop old, rename). `0001_InitialSchema.sql` is
  the pre-DbUp baseline (`IF NOT EXISTS`, so a no-op on databases created before DbUp).
- **Dates are stored/returned as UTC** regardless of server timezone (there's a
  regression test guarding this) — preserve that when touching the data layer.
- SQLite persistence stores dates as ISO `"O"` and decimals with `InvariantCulture`.

## Conventions

- The `TODO` file at the repo root tracks ongoing work and known issues.
