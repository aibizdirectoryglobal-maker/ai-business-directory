# Phase 1 verification — 17 October 2026

## Results

- .NET SDK 10.0.402 / ASP.NET Core runtime 10.0.13 installed in the user-local `%LOCALAPPDATA%\Codex\dotnet-10` directory. System SDK installation was not replaced.
- Debug build: succeeded with zero warnings and zero errors.
- Release publish: succeeded to ignored `dynamic/test-results/publish-20261017/`.
- Targeted HTTP/SQLite checks: **101 passed** against the development build, then **101 passed** against the Release publish output.
- Checks cover exact public source content and SEO preservation, public routes, shared/form CSS, local links and fragments, JSON-LD parsing, sitemap, verification file, staging robots/noindex, exclusion of private files, antiforgery, required fields, URL/email validation, consent, honeypot, length/body limits, HTML encoding, Pending-only saves, consent/version/UTC timestamps, redirect-after-save, restart preservation, canonical configuration, rate limiting and health checks.
- Production-mode HTTPS proxy simulation passed: secure/HttpOnly antiforgery cookie issuance and valid form submission with the documented forwarded-header environment setting.
- Release output inspection found no database files, local comparison experiment, hosted-provider setup document, test harness or source `.cs`/`.cshtml` files.
- SHA-256 comparisons of all 14 pre-existing website/document files matched the initial baseline. This includes the uncommitted homepage/listing prototype and original comparison experiment. Existing work was not rewritten.
- Disposable SQLite fixtures and published test output are retained under ignored `dynamic/test-results/`. No existing database was deleted or reset.

## Docker limitation

The Dockerfile and file-specific build-context allowlist were reviewed. Docker Desktop startup failed before the engine became usable. Its backend log reports an Inference manager `dockerInference` socket initialization error: “The file cannot be accessed by the system” / “The filename, directory name, or volume label syntax is incorrect.” The waiting build was cancelled. No factory reset, data deletion, image build, container run or deployment was performed.

Container execution remains **unverified**. After Docker Desktop is repaired separately, run the documented local Docker build and test before deployment approval. Passing native .NET checks is not a substitute for that container check.

## Files added

All new source/configuration/documentation is under `dynamic/`:

- `.gitignore`, `global.json`
- `Dockerfile`, `Dockerfile.dockerignore`
- `README.md`, `VERIFICATION.md`
- `AiBusinessDirectory.Web/AiBusinessDirectory.Web.csproj`
- `AiBusinessDirectory.Web/appsettings.json`
- `AiBusinessDirectory.Web/Program.cs`
- `AiBusinessDirectory.Web/PublicContent.cs`
- `AiBusinessDirectory.Web/Assets/listing.css`
- `AiBusinessDirectory.Web/Data/ListingDbContext.cs`
- `AiBusinessDirectory.Web/Pages/_ViewImports.cshtml`
- `AiBusinessDirectory.Web/Pages/RequestListing.cshtml`
- `AiBusinessDirectory.Web/Pages/RequestListing.cshtml.cs`
- `tests/SmokeTests/SmokeTests.csproj`
- `tests/SmokeTests/Program.cs`

No pre-existing project files were modified. Nothing was staged, committed, pushed or deployed. Render Free testing must remain disposable and non-indexable; persistent intake and a reviewer interface are outside this phase.
