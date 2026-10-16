# Local verification — 16 October 2026

## Original implementation verification

The following records the original checks before initial-upload preparation. Deployment metadata described as pending in this historical section has since been configured; see the initial-upload verification below.

Implementation and local verification complete. No repository creation, upload, or deployment performed. All writes were within this new project directory. Existing test data was not changed; the deployment-script test created a new, separate work copy.

## Checks performed

| Check | Result and scope |
| --- | --- |
| HTML structure | Passed a local Python HTMLParser-based nesting check on both pages, including one H1 and one main landmark per page, language, unique IDs, and a doctype. This is a lightweight structural check, not a full W3C conformance certification. |
| Metadata | Distinct, nonempty titles and descriptions on both pages. UTF-8 and responsive viewport metadata present. |
| Internal links | Every local link, stylesheet reference and fragment resolved to an existing target. Home-to-profile and profile-to-home navigation also exercised in the browser. |
| JSON-LD | Both blocks parsed as JSON. Schema.org context and intended types checked; ProfilePage mainEntity describes the business as an Organization and keeps its official URL. No rich-result eligibility claim is made. |
| Business content | Compared with the official website on 16 October 2026. Website-reported services only; no copied contact details, licenses, registration numbers, ratings, testimonials or street address. |
| CSS | Stylesheet loaded in the browser; 110 top-level CSS rules parsed, including media rules. Formatting-only cleanup preserved the rule count and rendered styles. |
| Responsive layout | Both pages checked in the Codex in-app browser at 320, 390, 768 and 1280 CSS-pixel viewport widths. No horizontal document overflow. Desktop home/profile and narrow home/profile layouts visually inspected. Temporary viewport override reset. Not a cross-browser or physical-device test. |
| HTTP preview | Local server returned HTTP 200 for home, business profile, stylesheet, robots.txt and sitemap.xml. Preview server stopped after verification. |
| GitHub Pages paths | Links are relative and were checked against a simulated repository subpath. No root-relative asset paths or build dependencies. `.nojekyll` included. Actual GitHub hosting remains untested. |
| Deployment configuration | Ran `configure-deployment.ps1` twice on a new work copy, using a reserved `.invalid` test address, never on the delivered source. Both passes produced one canonical per page, matching JSON-LD page URLs, two sitemap entries and the corresponding robots Sitemap directive. Official business URL stayed unchanged. No test URLs occur in delivered site HTML, sitemap or robots. |
| Staging sitemap and robots | XML parsed; sitemap intentionally has zero page entries pending the actual deployment URL. Robots permits crawling. These files must be finalised using the configuration script before launch. |

The verification used existing runtimes only; no packages were installed. Intermediate verification code and the isolated configuration test copy are retained under `work/` and excluded from the source archive.

## Remaining verification

1. Obtain the real Pages address after repository/deployment approval, then run the configuration script and review its output. Canonical tags and sitemap page entries are pending that address.
2. Confirm current business information with the official website before publication. This directory has not independently checked the business's credentials.
3. After approved deployment, check public HTTPS responses, navigation, stylesheet loading, canonical URLs, JSON-LD and sitemap; verify any origin-root robots policy. A project-path robots file does not govern the origin root.
4. A physical-phone check, other browser engines, an external Schema.org validator, and search-console inspection were not performed. Indexing and AI-powered discovery can only be assessed after publication and are not guaranteed.

## References

- [Official Golden Harbor website — factual source](https://goldenharborconsultancy.github.io/goldenharbor/)
- [GitHub Pages publishing-source instructions](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site)
- [Schema.org ProfilePage vocabulary](https://schema.org/ProfilePage)
- [Google Search Central robots.txt guidance](https://developers.google.com/search/docs/crawling-indexing/robots/intro)

## Initial-upload verification — 16 October 2026

- Confirmed the project directory and active GitHub CLI account `aibizdirectoryglobal-maker`.
- Confirmed the target repository was empty before initialization: GitHub reported size zero and no branches; `git ls-remote` returned no refs.
- Inspected and ran the existing deployment script with `https://aibizdirectoryglobal-maker.github.io/ai-business-directory/`.
- Rechecked both pages' JSON-LD syntax, canonical URLs and page identifiers, both sitemap entries, and the robots Sitemap directive against that base URL.
- Resolved every local HTML link, stylesheet and fragment under the repository subpath, including navigation from the business profile back to the homepage. Checked CSS for additional asset references.
- Checked intended source files for placeholder URLs, incorrect deployment paths, credentials and private information before staging.
- Reopened the official Golden Harbor website and confirmed that the organization's URL and published service summary match it. Only its public website was read; its repository was not accessed or modified.
- Used existing runtimes and no new dependencies. Existing data and original verification notes were retained.

This verification covers local source and repository-subpath HTTP checks. Earlier responsive checks are recorded above and were not repeated because the page layout and styles were unchanged. GitHub Pages settings and workflows remain untouched. Public HTTPS deployment and indexing verification remain pending a separately authorized hosting step.
