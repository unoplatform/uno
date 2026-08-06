# Algolia DocSearch configuration

This folder holds the **index/crawler-side** configuration for the docs search
(`platform` index, DocSearch v3, Algolia app `PHB9D8WS99`). It is the counterpart
to the display-layer code in `doc/templates/uno/partials/scripts.tmpl.partial` and
`doc/templates/uno/service/docsearch-transform.js`.

`docsearch-crawler-config.js` **mirrors the live crawler configuration** with the
two fixes from **unoplatform/uno-private#2038** applied. Background: unoplatform/uno#22788 / PR #22789.

## Why the fixes live here, not in the docs repo templates

DocSearch renders whatever records exist in the **hosted** Algolia index. The
crawler that produces those records runs on Algolia's servers and is **not**
executed by the docs build. So the fixes below are made in the hosted crawler /
index, using this file as the source of truth:

| Symptom | Layer | Mechanism | Status |
|---|---|---|---|
| Duplicate entries (one record per heading anchor) | Index setting | `attributeForDistinct: "url_without_anchor"` + `distinct: true` (was `"url"`) | **applied** |
| Nav / footer / sidebar / breadcrumb indexed as content | Crawler | `$(...).remove()` in `recordExtractor` | **applied** |
| Poor intent (e.g. "Get Started") | Index | `customRanking` (`weight.pageRank` first) | already present |
| `./page`, `./page/`, `./page/index.html` as separate hits | Crawler | canonical-URL collapsing in `recordExtractor` | optional (not yet applied) |
| Same-titled pages across sections | Index | `section` facet + `customRanking` | optional (not yet applied) |

> ⚠️ `data-docsearch-exclude` HTML attributes are **not** honored by the DocSearch
> crawler (verified against Algolia's docs). Exclusion must be done here
> (`$(...).remove()` / `selectors_exclude`), not by adding attributes to the docfx
> templates. The template markers remain only as intent documentation.

The client-side `transformItems` (`service/docsearch-transform.js`) is an interim
display polish — it only reshapes the already-returned top-N hits per group and
cannot fix ranking. Once this crawler config is live it can be trimmed further.

## How to apply

1. Open the crawler for app `PHB9D8WS99`: [dashboard.algolia.com](https://dashboard.algolia.com)
   → **Data sources → Crawlers** → the crawler whose target index is `platform`
   → **Editor**. Merge in the two changes from `docsearch-crawler-config.js`
   (the `$(...).remove()` block and `attributeForDistinct: "url_without_anchor"`),
   keeping your existing `appId` / `apiKey` / `startUrls` / `schedule`.
2. **Validate on a staging index first.** Set `indexPrefix: "staging_"` (indexes
   into `staging_platform`), **Save**, and run a crawl. Because the staging index
   is brand-new, `initialIndexSettings` (incl. `attributeForDistinct`) applies
   automatically. Inspect records / test queries against the acceptance checks below.
3. **Promote to production — two parts:**
   - **Records:** remove `indexPrefix` (→ `platform`) and run a crawl. See the
     safety-check note below.
   - **Settings:** `initialIndexSettings` is applied **only on the first crawl of a
     new index**, so the existing `platform` index will *ignore* the
     `attributeForDistinct` change. Set it on the index directly — **Search →
     Indices → `platform` → Configuration → Distinct** (attribute `url_without_anchor`,
     distinct on), or `setSettings { attributeForDistinct: "url_without_anchor",
     distinct: true }`. It takes effect immediately on existing records (they already
     carry `url_without_anchor`), so the dedup fix can ship without a re-crawl.

> ⚠️ **Safety check:** adding the chrome exclusions reduces the record count, so the
> first production crawl may exceed `safetyChecks.beforeIndexPublishing`
> (`maxLostRecordsPercentage: 30`) and refuse to publish. Raise it temporarily for
> that crawl, then restore.
>
> ⚠️ The `apiKey` in `docsearch-crawler-config.js` is a placeholder — it is a crawler
> **write** key and must never be committed. Set it in the dashboard.

## Acceptance checks (on the staging index)

- Searching a common term (e.g. **"MVUX"**, **"Get Started"**) returns **one row
  per page**, not one per heading.
- **No** navbar / footer / sidebar / breadcrumb text appears as a result.
- Same-titled pages across sections are ranked sensibly.
- (If canonical-URL collapsing is later enabled) a page reachable as both
  `.../page.html` and `.../page/index.html` appears once.

## Related

- Display layer: `doc/templates/uno/partials/scripts.tmpl.partial` (DocSearch init,
  `resultsFooterComponent`).
- Transform module + tests: `doc/templates/uno/service/docsearch-transform.js`,
  `docsearch-transform.test.js` (`node --test`).
- Full results page: `doc/search.html` (InstantSearch.js).
- References: DocSearch API <https://docsearch.algolia.com/docs/api/>,
  record extractor <https://docsearch.algolia.com/docs/record-extractor/>,
  `attributeForDistinct` <https://www.algolia.com/doc/api-reference/api-parameters/attributeForDistinct/>,
  `initialIndexSettings` <https://www.algolia.com/doc/tools/crawler/apis/configuration/initial-index-settings/>.
