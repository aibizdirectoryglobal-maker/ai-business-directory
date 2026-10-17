# Render Free deployment preparation — 17 October 2026

## Readiness

Ready for an **approved first remote Docker build and disposable test deployment**. No application/configuration blocker was identified. A working local Docker Desktop is not a prerequisite: Render builds repository Dockerfiles remotely with BuildKit. This is preparation, not evidence that a Render image build or service startup has already succeeded.

The earlier Phase 1 suggestion to repair Docker Desktop before proceeding is superseded for this remote-build path. Docker Desktop was not started, reset, reinstalled or otherwise changed during this phase.

## Minimal changes

- `dynamic/Dockerfile.dockerignore`: replaced the broad `dynamic/**` exception with exactly 15 required file exceptions. No directories or wildcard exceptions are admitted. This prevents future local secrets, development configuration, databases or prototypes from being swept into the build context. Docker automatically transmits its Dockerfile/ignore instructions separately as build metadata.
- `dynamic/Dockerfile`: added `-p:DebugType=None` to Release publishing to exclude PDB debug symbols from the runtime image.
- `dynamic/README.md`: made local Docker verification optional, documented remote build context and exact service settings, and linked this assessment.
- `dynamic/RENDER-READINESS.md`: this report.

No application handlers, configuration defaults, existing static pages, canonical URLs, SEO metadata or test fixtures were changed. Nothing was staged, committed, pushed, deployed or created on Render.

## Verification evidence

- Both Microsoft registry tags `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0` returned HTTP 200 manifests including Linux amd64 images. The project targets .NET 10 and EF Core SQLite 10.0.13; the official ASP.NET Core image source currently specifies runtime 10.0.13.
- An isolated source tree containing only the 15 admitted files successfully restored and published Release for `linux-x64`, framework-dependent, using .NET SDK 10.0.402. This verifies that the selected sources and repository-relative paths suffice without local build outputs or test files. It is not a Docker engine execution test.
- The final Linux publish contains 22 files, including `libe_sqlite3.so`. Inspection found no database files, secret/key files, PDBs, `.cs`/`.cshtml` sources, development configuration, test harness, original local comparison or hosted-provider setup document. All six copied public source files matched their source hashes. A recognizable credential/private-key signature scan of admitted inputs found no matches; this is a limited check, not a general secrets-audit guarantee.
- All **101 targeted application checks passed** again: public routes/assets/JSON-LD/local links, unchanged canonical/source content, health endpoint, staging noindex/robots, private-file exclusions, validation, antiforgery, honeypot, consent, Pending persistence, timestamps, preservation across local restarts, rate limits and HTTPS-proxy production form submission.
- `PORT` binds Kestrel to `0.0.0.0` and overrides the base image's default HTTP port. `/healthz` checks the database connection without returning submissions. The non-root runtime user owns `/app/data`.
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` is required on Render so its HTTPS-terminating ingress is recognized and secure antiforgery cookies work. This explicit cloud setting accepts forwarded headers without restricting proxy IPs; use it only behind Render ingress, not an exposed standalone HTTP listener. Canonicals do not derive from forwarded Host headers; the application limiter does not derive client identity from forwarded IPs.
- SQLite `EnsureCreated` initializes absent storage without deleting or reseeding existing records. It is not a schema-upgrade mechanism. Fresh Render test instances start with no submissions because no local database is copied.
- Hash verification confirms all 14 pre-existing website/document files, including uncommitted local work, remain unchanged. The staging area is empty. Existing local fixtures and new disposable test evidence are retained under ignored `dynamic/test-results/`.

## Exact settings after approval

Do not execute these steps until deployment and the necessary commit/push are approved.

1. Prepare the reviewed code on an approved **separate testing branch** in `https://github.com/aibizdirectoryglobal-maker/ai-business-directory`. Include the required public source files, the current reviewed homepage navigation, and the new application/build files. Render reads committed remote branch content, not this uncommitted local workspace. Do not push these preparations to the GitHub Pages production branch or change Pages settings. Do not include local databases, test evidence or the original local experiment in the proposed commit.
2. In Render select **New > Web Service**, connect that repository and select the approved testing branch. Service creation starts the initial deploy, so the final create/deploy button is part of the deployment approval boundary.
3. Apply the settings below. Leave custom domains, disks, database services, registry credentials and secret files unset. Do not enable paid add-ons. Choose Free and retain account usage/spending controls; Free resource selection does not remove workspace usage limits.

| Setting | Value |
| --- | --- |
| Runtime / Language | Docker |
| Root Directory | Empty — repository root |
| Docker Build Context Directory | `.` — if the field is shown |
| Dockerfile Path | `dynamic/Dockerfile` |
| Instance Type | Free |
| Auto-Deploy | Off |
| Health Check Path | `/healthz` |
| Docker Command / custom Start Command | Empty — use the Dockerfile ENTRYPOINT |
| Custom Build Command | Empty — Render uses the Dockerfile |
| Pre-Deploy Command | Empty — initialization happens at app startup |

| Environment variable | Exact value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` |
| `PublicSite__AllowIndexing` | `false` |
| `PublicSite__BaseUrl` | `https://aibizdirectoryglobal-maker.github.io/ai-business-directory/` |
| `Storage__DatabasePath` | `/app/data/listing-requests.test.db` |
| `SubmissionLimits__PermitLimit` | `20` |
| `SubmissionLimits__WindowSeconds` | `60` |
| `PORT` | Leave platform-assigned; Render's default is `10000` |

4. Only after approval, create/deploy the service and inspect the remote build/start logs. Check `/healthz`, `/`, `/businesses/golden-harbor.html`, `/comparisons/golden-harbor-vs-asm.html`, `/request-listing.html`, CSS, sitemap and verification file.
5. Confirm `X-Robots-Tag: noindex, nofollow, noarchive`, root `robots.txt` disallow-all, and unchanged GitHub Pages canonicals. Send a fictional request over the Render HTTPS URL and confirm the Pending acknowledgment; check an invalid request is rejected. Confirm private paths such as the local comparison return 404. Do not submit this temporary sitemap, add search-engine properties or redirect indexed pages.

## Remaining limitations

- Actual remote image construction, Linux container startup/non-root filesystem writes and Render ingress behavior remain unverified until the approved first deployment. The Linux cross-publish and native proxy simulation reduce uncertainty but do not prove those platform outcomes.
- Render account/repository authorization, Free quota availability and actual build capacity have not been inspected. If the account cannot select Free or lacks included capacity, stop rather than select a paid plan.
- Free service local files, including SQLite submissions and data-protection keys, are lost on restart, redeploy or idle spin-down (after 15 minutes without traffic). A cold start can take about a minute. Cookies/tokens may become invalid after keys disappear; reload the form. Use fictional/disposable data only. Render Free is not durable production intake.
- Submission limits are process-local. There is no reviewer interface or automatic publication. Persistent hosting later needs an approved durable store, backups and schema migration/reviewer security work.

## Official sources reviewed

- [Render Docker builds and BuildKit](https://render.com/docs/docker)
- [Repository-root and Docker context settings](https://render.com/docs/monorepo-support)
- [Render port binding and HTTPS ingress](https://render.com/docs/web-services#port-binding)
- [Render Free limits and ephemeral SQLite](https://render.com/docs/free)
- [Dockerfile-specific ignore files](https://docs.docker.com/build/concepts/context/#dockerignore-files)
- [ASP.NET Core forwarded HTTPS headers](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)
- [Official .NET 10 ASP.NET image](https://github.com/dotnet/dotnet-docker/blob/main/src/aspnet/10.0/noble/amd64/Dockerfile)
