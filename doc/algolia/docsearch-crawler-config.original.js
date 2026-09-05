/**
 * Algolia DocSearch — ORIGINAL crawler config for the `platform` docs index
 * (Algolia app PHB9D8WS99), i.e. the baseline that was live BEFORE the
 * unoplatform/uno-private#2038 fixes.
 *
 * Kept here as a one-paste ROLLBACK reference. It differs from the fixed config
 * (`docsearch-crawler-config.js`) ONLY by the three documented changes:
 *   • recordExtractor has NO `$(...).remove()` chrome exclusion and NO ?tabs
 *     query strip (it just returns `helpers.docsearch(...)`).
 *   • `attributeForDistinct` is `"url"` (not `"url_without_anchor"`).
 *   • no `ignoreQueryParams`.
 *
 * To roll back:
 *   1. Paste this into the Crawler Editor (restore your real `apiKey`) and crawl.
 *   2. Set `attributeForDistinct` back to `"url"` on the `platform` index
 *      directly (dashboard → Configuration → Distinct, or `setSettings`) — a
 *      crawl does NOT change settings on an already-existing index.
 *
 * NOTE: reconstructed by reversing the three changes above from the fixed config.
 * If you kept an export of the live pre-change config, diff against it before
 * relying on this for rollback (in particular the `initialIndexSettings`
 * metadata lists). `apiKey` is a placeholder — never commit the real write key.
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
      recordExtractor: ({ helpers }) => {
        return helpers.docsearch({
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
      },
    },
  ],
  safetyChecks: { beforeIndexPublishing: { maxLostRecordsPercentage: 30 } },
  initialIndexSettings: {
    platform: {
      attributesForFaceting: ["type", "lang"],
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
      attributeForDistinct: "url",
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
