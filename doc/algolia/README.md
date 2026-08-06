# Algolia DocSearch configuration

This folder holds the **index/crawler-side** configuration for the docs search
(`platform` index, DocSearch v3). It is the counterpart to the display-layer code
in `doc/templates/uno/partials/scripts.tmpl.partial` and
`doc/templates/uno/service/docsearch-transform.js`.

Background and rationale: **unoplatform/uno-private#2038** (recap of
unoplatform/uno#22788 / PR #22789).

## Why the fixes live here, not in the docs repo templates

DocSearch renders whatever records exist in the **hosted** Algolia index. The
crawler configuration that produces those records runs on Algolia's servers and
is **not executed by the docs build**. As a result:

| Symptom | Correct layer | Mechanism (see `docsearch-crawler-config.js`) |
|---|---|---|
| Duplicate entries (one record per heading) | Index | `attributeForDistinct: 'url_without_anchor'` + `distinct: 1` |
| `./page`, `./page/`, `./page/index.html` as separate hits | Crawler | URL canonicalization in `recordExtractor` |
| Nav / footer / sidebar / breadcrumb indexed as content | Crawler | `$(...).remove()` in `recordExtractor` |
| Same-titled pages across sections | Index | `section` facet + `customRanking` |
| Poor intent (e.g. "Get Started") | Index | `customRanking` (`weight.pageRank` first) |

> ⚠️ **`data-docsearch-exclude` HTML attributes are NOT honored by the DocSearch
> crawler.** Excluding chrome must be done here (`selectors_exclude` / `.remove()`),
> not by adding attributes to the docfx templates. The attribute additions in the
> original PR have no effect on the index.

The client-side `transformItems` (in `service/docsearch-transform.js`) remains
only as an **interim display polish** — it operates on the already-returned
top-N hits per group and cannot recover a result that a duplicate displaced in
ranking. Once this crawler config is live, that module can be trimmed further.

## How to apply

1. Open the **Algolia Crawler** for the DocSearch account that owns app
   `PHB9D8WS99` (Crawler → Editor), or use the Crawler API.
2. Paste `docsearch-crawler-config.js`. Provide the **crawler (write) API key**
   from the dashboard / secret store — it is intentionally a placeholder here and
   must never be committed.
3. **Validate on a staging index first** (do not touch production):
   - Set `indexPrefix: 'staging_'` (indexes as `staging_platform`).
   - Run the crawler and inspect the records / test queries in the dashboard.
   - Point a local docs build at the staging index by temporarily changing
     `indexName` in `scripts.tmpl.partial` to `staging_platform`.
4. Verify against the acceptance checks below, then promote to `platform`.

## Acceptance checks (run on the staging index)

- Searching a common term (e.g. **"MVUX"**, **"Get Started"**) returns **one row
  per page**, not one per heading.
- **No** navbar / footer / sidebar / breadcrumb text appears as a result, and no
  `#breadcrumb`-anchored URLs are present.
- A page reachable as both `.../page.html` and `.../page/index.html` appears once.
- Same-titled pages in different sections are distinguishable (different
  `section` facet value) and ranked sensibly.

## Related

- Display layer: `doc/templates/uno/partials/scripts.tmpl.partial`
  (DocSearch init, `resultsFooterComponent`).
- Transform module + tests: `doc/templates/uno/service/docsearch-transform.js`,
  `docsearch-transform.test.js` (`node --test`).
- Full results page: `doc/search.html` (InstantSearch.js).
- References: DocSearch API <https://docsearch.algolia.com/docs/api/>,
  record extractor <https://docsearch.algolia.com/docs/record-extractor/>,
  `attributeForDistinct` <https://www.algolia.com/doc/api-reference/api-parameters/attributeForDistinct/>.
