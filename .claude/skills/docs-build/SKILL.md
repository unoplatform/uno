---
name: docs-build
description: Build, preview, and validate the Uno documentation website (DocFX) locally — surface real content errors vs expected noise, drive rendered pages with Playwright, and validate external-doc commit bumps in import_external_docs.ps1 before a PR. Use when working under doc/, editing articles/** markdown, bumping an external docs ref (uno.themes, studio-docs, hd-docs, uno.chefs, workshops, …), or rendering/validating the docs site.
---

# Docs build & validation (DocFX)

Validate that the documentation is **correct** after a change: surface DocFX errors/warnings, drive
the rendered site in a real browser, and tell real content problems apart from expected noise.
Everything is under `doc/` and is **independent of the Uno.UI build** — no
`crosstargeting_override.props` needed.

## TL;DR validation loop

```bash
cd doc
docfx --version                                   # MUST match build/Uno.UI.Build.csproj <DocfxVersion>; see Gotchas
pwsh -NoProfile -Command "./import_external_docs.ps1"   # only if you changed external pins
docfx docfx.json 2>&1 | sed -E 's/\x1b\[[0-9;]*m//g' > /tmp/docfx.log   # capture clean (no color) log
grep -iE "error|warning" /tmp/docfx.log | sort | uniq -c | sort -rn      # triage
```
Then filter to the part you changed (§2), drive the rendered pages in a browser (§3), and confirm
they exist in `_site`.

## 1. The error/warning taxonomy — what each one means

DocFX prints `error` and `warning` lines. The ones that indicate a **real content problem you
must fix** (and that block/break a page):

| Signal | Meaning | How to fix |
|--------|---------|------------|
| `InvalidFileLink: Invalid file link:(~/…)` | A relative link to a `.md`/image/path that doesn't exist | Fix the link target or add the missing file. Watch for links to `.html` that should point to `.md`, and links missing an extension. |
| `UidNotFound: … invalid cross reference(s) "X"` | An `xref:X` / `@X` / `<xref>` points to a UID no page defines | Fix the UID, or ensure the page defining it is included in the build. Common after renaming a `uid:` or removing a page. |
| `InvalidTocInclude: Referenced TOC file … does not exist` | A `toc.yml` `href:` includes another `toc.yml` that isn't there | Fix the path, or import the external repo that provides it. (`articles/implemented/toc.yml` missing is **expected locally** — see §4.) |
| `DuplicateUids: Uid(X) has already been defined in …` | Two pages declare the same `uid:` | Make UIDs unique; frequent when copying templates across `workshops` module variants. |
| `invalid-tab-group: Tab group with different tab id set` | Inconsistent `# [Tab](#tab/…)` ids within one group | Align the tab ids across the tab group. |
| Build does not reach `Build succeeded` / non-zero exit | A fatal error aborted the build | Read the last error in the log; fix before anything else. |

**The change you make should not introduce any new line of these types referencing your files.**

## 2. Validate a specific change (diff against the noise)

The site has a baseline of pre-existing warnings unrelated to your change. The reliable method is
to **scope the output to what you touched**, not to read the whole log.

- **External-doc commit bump** (e.g. `studio-docs`): after `import_external_docs.ps1`, expect
  **zero** issues mentioning that repo:
  ```bash
  grep -i "external/<repo>" /tmp/docfx.log            # expect: no error/warning lines
  find _site/articles/external/<repo>/ -name "*.html" # expect: the pages rendered
  ```
- **In-repo markdown edit**: grep the log for your file path(s), and click through the rendered
  page(s) under `_site/…`.
- **When unsure if a warning is new**: capture a baseline log on `master` (or before your change),
  then `diff` the two triaged lists. Only lines new to your branch matter.

## 3. Runtime validation in a browser (Playwright — recommended)

A clean DocFX log proves the pages *built*; Playwright proves they *render*. After serving the
site (`dotnet-serve --directory _site --port 8088`, or `npm start` on `:3000`), drive the pages you
changed and assert on real browser behavior. Use the Playwright MCP browser tools if available, or
`npx playwright`. For each changed/affected page, check:

1. **Loads with HTTP 200** and the expected `<h1>`/title is present (not a DocFX 404 or empty body).
2. **No broken images** — every `<img>` has `naturalWidth > 0`.
3. **No dead in-page links** to other doc pages (sample the nav/TOC links → 200, not 404).
4. **No console errors** in the page (`page.on('console')` / the MCP console-messages tool).
5. **Visual spot-check** — take a screenshot and confirm layout, styles loaded (not unstyled HTML),
   code blocks/tabs/mermaid render.

Minimal example (adapt the URL to your changed page):
```js
const { chromium } = require('playwright');     // or use the Playwright MCP browser tools
const page = await (await chromium.launch()).newPage();
const errors = [];
page.on('console', m => m.type() === 'error' && errors.push(m.text()));
const resp = await page.goto('http://localhost:8088/articles/external/studio-docs/App/uno-platform-studio-get-started.html');
console.assert(resp.status() === 200, 'page did not load');
console.assert(await page.locator('h1').count() > 0, 'no heading rendered');
const broken = await page.$$eval('img', imgs => imgs.filter(i => !i.naturalWidth).map(i => i.src));
console.assert(broken.length === 0, 'broken images: ' + broken);
console.assert(errors.length === 0, 'console errors: ' + errors);
await page.screenshot({ path: '/tmp/doc-page.png', fullPage: true });
```
Report findings with explicit labels: **Build** (DocFX log clean) vs **Runtime** (Playwright loaded
the page and asserted). Don't present a clean build as runtime proof.

## 4. Expected noise — do NOT chase these locally

These appear on a clean checkout and are **not** caused by your change:

- `InvalidTocInclude … articles/implemented/toc.yml does not exist` and an empty/placeholder
  implemented TOC — the API "implemented views" are generated by CI only.
- `InvalidFileLink`/`UidNotFound`/`DuplicateUids` already present in unrelated areas (notably
  `workshops`, `silverlight-migration`, feature includes). Confirm they don't reference your files.
- **`ApplyTemplateRendererError … maximum recursion limit of 256`** when it hits **every** `toc.yml`
  → this is a **DocFX version bug** (newer versions), not content. Pin DocFX to the CI version
  (Gotcha #1) and it disappears.

## 5. Gotchas

1. **Pin DocFX to the CI version.** Read `<DocfxVersion>` from `build/Uno.UI.Build.csproj`. The
   default `dotnet tool update --global docfx` installs the latest, which can emit the false
   `recursion limit of 256` on every TOC (see §4).
   ```bash
   docfx --version
   dotnet tool uninstall --global docfx
   dotnet tool install   --global docfx --version <DocfxVersion>
   ```
2. **Do NOT validate a pinned commit with `import_external_docs_test.ps1`.** That wrapper forces
   each repo to its **branch** (`studio-docs = "main"`, …), so it ignores the `ref=` you committed.
   Run `import_external_docs.ps1` **directly** (no args) to honor the committed pins.

## 6. Build mechanics (reference)

CI source of truth: `build/ci/docs/.azure-devops-docs.yml` → MSBuild target `GenerateDoc` in
`build/Uno.UI.Build.csproj` runs (≈ lines 111-134): sync-generator (skip locally) →
`import_external_docs.ps1` → `generate-llms-full.ps1` → install pinned DocFX → `docfx docfx.json`.
Styles/scripts come from `npm install && npm run build` (= `gulp build`, which also runs DocFX),
output to `doc/_site`.

To preview with styles: `npm install` then `npm run build`, then
`dotnet-serve --directory _site --port 8088` (or `npm start` to build + live-serve on `:3000`).

Generated/gitignored — never commit: `doc/_site`, `doc/styles`, `doc/node_modules`,
`doc/articles/external/**`. Only the `ref=` pins in `import_external_docs.ps1` are committed.

For markdown style there are also CI lint checks you can run locally:
`cspell --config ./cSpell.json "doc/**/*.md" --no-progress` and `markdownlint "doc/**/*.md"`
(see `doc/README.md`).
