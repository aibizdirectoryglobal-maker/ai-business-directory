# Dynamic application — local and Render Free testing

The existing repository-root static website is preserved. The new application is entirely under `dynamic/` and does not use a hosted form processor. No deployment, domain, paid service, production database or automatic publication is configured.

## Runtime and local start

Use a .NET 10 SDK. The normal system .NET 9 SDK cannot build this project. If a user-local SDK was installed during preparation, its executable is `%LOCALAPPDATA%\Codex\dotnet-10\dotnet.exe`; it does not replace the system SDK.

From the repository root in PowerShell:

```powershell
$dotnet10 = "$env:LOCALAPPDATA\Codex\dotnet-10\dotnet.exe"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
& $dotnet10 run --project dynamic/AiBusinessDirectory.Web -- --urls http://127.0.0.1:5080
```

If .NET 10 is installed normally, `dotnet` can replace `& $dotnet10`. Open `http://127.0.0.1:5080/` and `/request-listing.html`. Use fictional data only. Stop with Ctrl+C. The default database is `dynamic/AiBusinessDirectory.Web/App_Data/listing-requests.test.db`. Creating a database does not delete, reset or reseed an existing database.

The browser-facing request page is a Razor Page. Its POST handler requires an antiforgery token, validates all fields and consent on the server, rejects a filled honeypot, stores a Pending request, and redirects to a confirmation. There is no public submission list, approval UI, email delivery, automatic approval or public profile generation in Phase 1.

The application-wide submission limiter defaults to 20 POSTs per 60 seconds with no queue and does not use forwarded client-IP headers to identify clients. A 32 KiB request-body limit and field-length limits also apply. Cookie security uses HTTPS in Production; local HTTP tests must use Development. On Render, TLS terminates at the platform and the public URL uses HTTPS. Enable ASP.NET Core's forwarded-header support there so secure antiforgery cookies work through that proxy; enable it only behind the hosting platform's trusted ingress, not on an independently exposed HTTP server.

## Source publication allowlist

MSBuild copies only these repository-root files into the application's build/publish output:

- `index.html`
- `businesses/golden-harbor.html`
- `comparisons/golden-harbor-vs-asm.html`
- `assets/styles.css`
- `sitemap.xml`
- `google33bc54793ff2be2c.html`

The new Razor page and its scoped CSS are additional app content. The old `request-listing.html`, local experiment, setup documents, Git metadata, testing data and database files are not served as static content. The homepage copy includes existing uncommitted local navigation, but its source is never rewritten.

## Environment variables

| Variable | Default / meaning |
| --- | --- |
| `PORT` | When supplied, binds HTTP on `0.0.0.0` at that assigned port. Otherwise use standard ASP.NET Core URL settings. |
| `ASPNETCORE_ENVIRONMENT` | `Production` in Docker. Use `Development` for local HTTP form testing. |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Set `true` on Render's HTTPS-terminating ingress. Uses the framework's forwarded-header middleware; do not enable on an unprotected public HTTP server. |
| `Storage__DatabasePath` | `App_Data/listing-requests.test.db` relative to content root locally; `/app/data/listing-requests.test.db` in Docker. An absolute path can later point at an approved persistent disk. |
| `PublicSite__BaseUrl` | Existing GitHub Pages canonical base URL. Explicitly changes copied canonical URLs, directory JSON-LD URLs and sitemap entries in responses; never inferred from the incoming Host header. |
| `PublicSite__AllowIndexing` | `false`. Adds `X-Robots-Tag: noindex, nofollow, noarchive` and returns a disallow-all root robots file. Keep false throughout Phase 1. Temporary `onrender.com` URLs are rejected if indexing is requested. |
| `SubmissionLimits__PermitLimit` | `20`, application-wide listing POSTs per window. |
| `SubmissionLimits__WindowSeconds` | `60`. |

The default copied SEO metadata retains the existing GitHub Pages canonical URLs. Do not replace them with a temporary Render hostname. The new app does not redirect the existing website, change GitHub Pages, or submit a sitemap to any search engine. `/healthz` returns 200 only when the database connection works; it exposes no request records or filesystem paths.

## Targeted local verification

```powershell
& $dotnet10 build dynamic/tests/SmokeTests/SmokeTests.csproj
& $dotnet10 run --project dynamic/tests/SmokeTests/SmokeTests.csproj --no-build
```

The console smoke tests start only local child processes and use unique databases under ignored `dynamic/test-results/`. They verify routes, exact copied source content, SEO staging headers, private-path exclusions, antiforgery, required fields, URL/email validation, consent, honeypot, field/body limits, stored Pending records and timestamps, preservation through restart, configurable canonical URLs and rate limiting. Test artifacts are retained, not deleted.

Phase 1 results are recorded in [VERIFICATION.md](VERIFICATION.md). Phase 2 remote-build preparation is recorded in [RENDER-READINESS.md](RENDER-READINESS.md). Local Docker Desktop is optional: Render can build this Dockerfile remotely using BuildKit. The first actual container build and run remain unverified until an approved remote deployment or a separate working Docker engine is available.

The Phase 1 schema is initialized with EF Core `EnsureCreated`, which creates missing storage but never drops a database. This is not an automatic schema-upgrade mechanism. Before any future model changes or real persistent usage, introduce a reviewed migration strategy with a tested backup; never use `EnsureDeleted` or overwrite an existing database.

## Optional local Docker verification

These optional commands require a working local Linux engine. Do not reset or reinstall Docker Desktop to complete Render preparation. From the repository root:

```powershell
docker build -f dynamic/Dockerfile -t ai-business-directory-phase1 .
docker run --rm -p 5080:10000 -e PORT=10000 -e ASPNETCORE_ENVIRONMENT=Development ai-business-directory-phase1
```

The Dockerfile-specific ignore file starts by excluding everything and then admits exactly 15 required source files. This excludes Git metadata, local experiments, databases, secret files, development configuration, build outputs and tests. Docker also transmits its Dockerfile/ignore instructions as build metadata. The SDK stage compiles those allowed sources; the final runtime stage receives only published application artifacts, without source files or PDB debug symbols. The image uses .NET 10 SDK/runtime images, runs as the non-root `app` user and writes test storage to `/app/data`. `--rm` is appropriate only for these disposable container tests; it is not a production persistence configuration.

## Exact next steps for Render Free — approval required

1. Obtain approval before committing/pushing application code or creating a Render service. Use a separately approved branch that is not the GitHub Pages production publishing branch. Do not push to `main` merely to test Render: that can trigger the existing Pages deployment.
2. In Render, choose **New > Web Service**, connect the intended GitHub account/repository and select that approved testing branch.
3. Choose **Docker** as the runtime/language, leave **Root Directory** empty, set **Docker Build Context Directory** to `.` (repository root, if this field is shown), and set **Dockerfile Path** to `dynamic/Dockerfile`. Render builds the image remotely; no local image or registry credentials are required for these public Microsoft base images. Do not point the root directory at `dynamic/`: the allowlisted public sources are outside it.
4. Select the **Free** instance type. Do not create a database service or attach a disk. Keep automatic deployment disabled until the test setup is approved.
5. Set `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, `PublicSite__AllowIndexing=false`, `PublicSite__BaseUrl=https://aibizdirectoryglobal-maker.github.io/ai-business-directory/`, and `Storage__DatabasePath=/app/data/listing-requests.test.db`. Let Render assign `PORT` (normally 10000); the app reads it directly.
6. Set **Health Check Path** to `/healthz`. Leave **Docker Command**, **Pre-Deploy Command** and any custom build/start command empty; the Dockerfile handles restore/publish/startup, and the app initializes missing SQLite storage. No secret files or credentials are needed. Do not configure additional databases, disks, domains or paid add-ons.
7. Only after approval, create/deploy the Free service. Verify `/`, both preserved `.html` routes, `/request-listing.html`, assets and `/healthz`, plus the noindex response header and disallow-all robots file. Use fictional test requests only. Do not submit the Render sitemap or redirect existing indexed pages.

## Render Free and persistence limitations

Render Free has no persistent disk. Its local SQLite data and authentication key files can disappear on restart, redeploy or idle spin-down. The app never deliberately deletes them, but the hosting platform does not preserve them. This is a test application, not a production listing intake service. The limiter is also process-local and resets on restart. Startup can be delayed after idle periods. Do not use uptime-pinging workarounds or assume the Free service provides durable storage.

Later persistent hosting only requires changing the configured storage path to a real durable location, but it also needs backups, schema migrations, reviewer authentication and a separate production-readiness review. All these are outside Phase 1.

Official references: [Render Free](https://render.com/docs/free), [port binding](https://render.com/docs/web-services#port-binding), [Docker](https://render.com/docs/docker).
