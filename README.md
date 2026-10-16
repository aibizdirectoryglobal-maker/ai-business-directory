# AI Business Directory — Phase 1

A buildless HTML/CSS proof of concept. No JavaScript, frameworks, external images, fonts, runtime packages, forms, or tracking. The source is prepared for the AI Business Directory repository. GitHub Pages has not been enabled.

## Files

```text
ai-business-directory/
├── index.html
├── businesses/
│   └── golden-harbor.html
├── assets/
│   └── styles.css
├── robots.txt
├── sitemap.xml
├── .nojekyll
├── configure-deployment.ps1
├── README.md
└── LOCAL-VERIFICATION.md
```

- **Home:** introduces the directory, its discovery purpose, its first business, and the limits of this proof of concept.
- **Business profile:** summarises Golden Harbor's business, country, category and services, with source attribution and official website links. Its GH monogram is directory typography, not a claimed official logo.

## Factual source

[Golden Harbor Consultancy's official website](https://goldenharborconsultancy.github.io/goldenharbor/), reviewed 16 October 2026. The profile reports the business's own statements; it does not certify credentials. No contact details, registration numbers, licenses, ratings, testimonials, or street addresses are reproduced. The source repository has not been accessed or modified.

## Discoverability

Both pages contain readable business information in their delivered HTML, unique titles and descriptions, semantic headings, and JSON-LD. The homepage uses `WebSite`; the business profile uses `ProfilePage` with an `Organization` main entity. The organization's `url` points to the official business website. Relative navigation and stylesheet paths work below a GitHub Pages repository path.

**Deployment URLs are configured** for `https://aibizdirectoryglobal-maker.github.io/ai-business-directory/`. Both canonical tags and JSON-LD page URLs use this repository subpath. The sitemap lists both pages, and robots.txt includes its sitemap URL. This is the expected hosting address; GitHub Pages is not enabled yet.

## Local viewing

Open `index.html` directly in a browser. For HTTP preview, from this folder use an existing Python installation:

```powershell
python -m http.server 8000 --bind 127.0.0.1
```

Open [the local preview](http://127.0.0.1:8000/). Stop it with Ctrl+C. No build step is required.

## Repository and future publishing

Source repository: [AI Business Directory](https://github.com/aibizdirectoryglobal-maker/ai-business-directory).

The initial upload targets `main`. GitHub Pages settings and deployment workflows are outside the initial-upload scope. Enabling hosting requires separate authorization.

To regenerate deployment metadata from this folder:

```powershell
.\configure-deployment.ps1 -BaseUrl 'https://aibizdirectoryglobal-maker.github.io/ai-business-directory/'
```

The script only updates local files. It accepts the URL with or without its trailing slash and supports the repository subpath. Review canonical tags, JSON-LD page URLs, sitemap entries and robots Sitemap directive after running it. Preserve the profile organization's official website URL. Keep `.nojekyll` at the repository root. See `LOCAL-VERIFICATION.md` for verification scope.

After a separately authorized Pages deployment, verify both pages, CSS, navigation and metadata over public HTTPS. No public hosting or indexing success is claimed by this source upload.

## Public deployment limitations

For a GitHub Pages project site, `robots.txt` lives below the repository path. Crawlers generally consult the origin's root `/robots.txt`; the repository file cannot control that root. Do not change another repository to fix this without authorization. The sitemap can be submitted directly using its actual deployed URL. A root-level robots configuration or custom-domain choice remains a deployment decision.

No claims are made about actual indexing, AI crawler access, AI answers, search rankings, or partnerships. Confirm source information again before launch; this is a dated source review, not continuing monitoring.
