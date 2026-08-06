/**
 * Algolia DocSearch — Crawler configuration for the `platform` docs index
 * (Algolia app PHB9D8WS99).
 *
 * This mirrors the LIVE crawler configuration, with the fixes from
 * unoplatform/uno-private#2038 applied:
 *
 *   CHANGE 1 — `recordExtractor` now strips site chrome (navbar / footer /
 *   sidebar / breadcrumb / affix / TOC) via `$(...).remove()` so it is not
 *   indexed as searchable content. Note: `data-docsearch-exclude` HTML
 *   attributes are NOT honored by the DocSearch crawler — exclusion must be
 *   done here, server-side.
 *
 *   CHANGE 2 — `attributeForDistinct` is `url_without_anchor` (was `url`) so a
 *   page yields ONE result instead of one per heading anchor. This is the
 *   duplicate-results fix.
 *
 *   CHANGE 3 — `recordExtractor` strips the query string (e.g. ?tabs=…) from
 *   record URLs so docfx tabbed pages index under one clean URL. This is done
 *   in the extractor, NOT via the crawler's `ignoreQueryParams` — that option
 *   infinite-loops against the pages' client-side ?tabs redirect and drops them.
 *
 * This file is NOT executed by the docs build; it is the source of truth for the
 * hosted Algolia Crawler. Apply it via the Crawler dashboard (Data sources →
 * Crawlers → Editor) or the Crawler API. Validate on a staging index first by
 * setting `indexPrefix: "staging_"`.
 *
 * Operational notes (see README.md in this folder):
 *   • `initialIndexSettings` is applied ONLY on the first crawl of a NEW index.
 *     The existing `platform` index therefore ignores the `attributeForDistinct`
 *     change from a crawl — set it directly on the index instead
 *     (dashboard → Configuration → Distinct, or `setSettings`
 *     { attributeForDistinct: "url_without_anchor", distinct: true }). It takes
 *     effect immediately on existing records (they already carry
 *     `url_without_anchor`); no re-crawl is needed for the dedup half.
 *   • Adding the chrome exclusions reduces the record count, so the first
 *     production crawl may trip `safetyChecks.beforeIndexPublishing`
 *     (maxLostRecordsPercentage: 30). Raise it temporarily for that crawl,
 *     then restore.
 *   • `apiKey` below is a crawler WRITE key — it is intentionally a placeholder.
 *     Set the real value in the Crawler dashboard; never commit it.
 *
 * Optional enhancements (NOT deployed) — a `section` facet for same-title
 * disambiguation, and `/index.html` / trailing-slash canonicalization — are
 * provided as ready-to-apply snippets in README.md ("Optional enhancements").
 */
new Crawler({
  rateLimit: 8,
  maxDepth: 10,
  maxUrls: 5000,
  startUrls: ["https://platform.uno/docs/articles/intro.html"],
  renderJavaScript: true,
  ignoreCanonicalTo: false,
  discoveryPatterns: ["https://platform.uno/docs/**"],
  schedule: "at 15:10 on Tuesday",
  actions: [
    {
      indexName: "platform",
      pathsToMatch: ["https://platform.uno/docs/**"],
      recordExtractor: ({ $, helpers }) => {
        // CHANGE 1: strip site chrome so navbar/footer/sidebar/breadcrumb are
        // not indexed as searchable content.
        $(
          "#header-container, header, nav.navbar, footer, .subnav, " +
          "#breadcrumb, .breadcrumb, .level1.breadcrumb, .sidetoc, .sidefilter, " +
          ".sidenav, .sideaffix, #affix, .affix, [role='complementary'], #docsearch, .toc, #toc"
        ).remove();

        const records = helpers.docsearch({
          recordProps: {
            lvl1: [
              "header h1",
              "article h1",
              "main h1",
              "h1",
              ".level1.breadcrumb li:last()",
              "head > title",
            ],
            content: ["article p, article li"],
            lvl0: {
              selectors: "",
              defaultValue: "Documentation",
            },
            lvl2: ["article h2", "main h2", "h2"],
            lvl3: ["article h3", "main h3", "h3"],
            lvl4: ["article h4", "main h4", "h4"],
            lvl5: ["article h5", "main h5", "h5"],
            lvl6: ["article h6", "main h6", "h6"],
          },
          aggregateContent: false,
          recordVersion: "v3",
        });

        // CHANGE 3: strip the query string (e.g. ?tabs=…) so docfx tabbed pages
        // index under one clean, query-free URL (the #anchor is kept). Done here
        // in the extractor rather than via the crawler's `ignoreQueryParams`:
        // that option infinite-loops against these pages' client-side ?tabs
        // redirect (renderJavaScript: true) and would drop every tabbed page.
        const stripQuery = (u) => String(u || "").replace(/\?[^#]*/, "");
        // OPTIONAL (not deployed): swap `stripQuery` for a `canonicalize` that
        // also collapses /index.html + trailing slashes, and attach a `section`
        // facet — ready-to-apply snippets in README.md ("Optional enhancements").
        return records.map((r) => ({
          ...r,
          url: stripQuery(r.url),
          url_without_anchor: stripQuery(r.url_without_anchor),
        }));
      },
    },
  ],
  safetyChecks: { beforeIndexPublishing: { maxLostRecordsPercentage: 30 } },
  initialIndexSettings: {
    platform: {
      attributesForFaceting: ["type", "lang"], // + "filterOnly(section)" to enable the section-facet enhancement (README)
      attributesToRetrieve: [
        "hierarchy",
        "content",
        "anchor",
        "url",
        "url_without_anchor",
        "type",
      ],
      attributesToHighlight: ["hierarchy", "content"],
      attributesToSnippet: ["content:10"],
      camelCaseAttributes: ["hierarchy", "content"],
      searchableAttributes: [
        "unordered(hierarchy.lvl0)",
        "unordered(hierarchy.lvl1)",
        "unordered(hierarchy.lvl2)",
        "unordered(hierarchy.lvl3)",
        "unordered(hierarchy.lvl4)",
        "unordered(hierarchy.lvl5)",
        "unordered(hierarchy.lvl6)",
        "content",
      ],
      distinct: true,
      attributeForDistinct: "url_without_anchor", // CHANGE 2: was "url"
      customRanking: [
        "desc(weight.pageRank)",
        "desc(weight.level)",
        "asc(weight.position)",
      ],
      ranking: [
        "words",
        "filters",
        "typo",
        "attribute",
        "proximity",
        "exact",
        "custom",
      ],
      highlightPreTag: '<span class="algolia-docsearch-suggestion--highlight">',
      highlightPostTag: "</span>",
      minWordSizefor1Typo: 3,
      minWordSizefor2Typos: 7,
      allowTyposOnNumericTokens: false,
      minProximity: 1,
      ignorePlurals: true,
      advancedSyntax: true,
      attributeCriteriaComputedByMinProximity: true,
      removeWordsIfNoResults: "allOptional",
    },
  },
  appId: "PHB9D8WS99",
  apiKey: "REPLACE_WITH_CRAWLER_WRITE_KEY", // set in the Crawler dashboard — never commit the real key
});
