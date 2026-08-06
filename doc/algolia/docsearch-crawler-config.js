/**
 * Algolia DocSearch — Crawler configuration for the `platform` docs index.
 *
 * WHY THIS FILE EXISTS
 * --------------------
 * Per unoplatform/uno-private#2038, search duplicates, navigation noise and
 * ranking are an *indexing* concern, not a display concern. DocSearch renders
 * whatever records the hosted index contains; the client (`transformItems`) can
 * only reshape the already-returned top-N hits. The real fixes therefore live
 * here, in the crawler/index configuration:
 *
 *   - Nav / footer / sidebar / breadcrumb excluded from indexing  -> `.remove()`
 *   - One record per page (collapse per-heading duplicates)        -> `attributeForDistinct` + `distinct`
 *   - `./page`, `./page/`, `./page/index.html` collapsed           -> URL canonicalization
 *   - Same-titled pages across sections disambiguated / ranked     -> `section` facet + `customRanking`
 *
 * NOTE: `data-docsearch-exclude` HTML attributes are NOT honored by the crawler;
 * exclusion must be done here (server-side). See #2038.
 *
 * HOW TO APPLY
 * -----------
 * This configuration is NOT executed by the docs build. It is the source of
 * truth for the crawler that Algolia hosts for the DocSearch program. Apply it
 * via the Algolia Crawler dashboard (Crawler > Editor) or the Crawler API for
 * the `platform` index. Validate on a STAGING index first — see README.md in
 * this folder. `apiKey` below is a *crawler* (write) key and must be supplied
 * from the dashboard / a secret store — never commit it.
 */
new Crawler({
  appId: 'PHB9D8WS99',
  apiKey: 'CRAWLER_API_KEY', // write key — set in the Crawler dashboard, do NOT commit
  indexPrefix: '', // set to e.g. 'staging_' when validating on a staging index
  rateLimit: 8,
  maxDepth: 12,
  maxUrls: 20000,
  startUrls: ['https://platform.uno/docs/'],
  sitemaps: ['https://platform.uno/docs/sitemap.xml'],
  renderJavaScript: false,
  // Respect <link rel="canonical">; docfx pages that emit one will collapse
  // duplicate URLs automatically. We also canonicalize defensively below.
  ignoreCanonicalTo: false,
  discoveryPatterns: ['https://platform.uno/docs/**'],
  // Do not index redirect stubs, raw TOC fragments or the API-diff churn pages.
  exclusionPatterns: [
    'https://platform.uno/docs/**/toc.html',
    'https://platform.uno/docs/**/toc.json',
  ],
  schedule: 'every 1 day at 3:00 am',

  actions: [
    {
      indexName: 'platform',
      pathsToMatch: ['https://platform.uno/docs/**'],
      recordExtractor: ({ $, url, helpers }) => {
        // --- Server-side exclusion of chrome (replaces the ineffective
        //     `data-docsearch-exclude` attributes). Strip everything that is
        //     not article content BEFORE extraction. ---
        $(
          [
            '#header-container',
            'header',
            'nav.navbar',
            'footer',
            '.subnav',
            '#breadcrumb',
            '.breadcrumb',
            '.sidetoc',
            '.sidefilter',
            '.sidenav',
            '.sideaffix',
            '#affix',
            '.affix',
            '[role="complementary"]',
            '#docsearch',
            '.toc',
            '#toc',
            '.nextstepaction',
          ].join(', ')
        ).remove();

        const records = helpers.docsearch({
          recordProps: {
            lvl0: {
              // docfx emits no per-section label inside the article, so lvl0 is a
              // constant (matches the DocSearch default template). Cross-section
              // disambiguation is done via the `section` facet attached below.
              selectors: '',
              defaultValue: 'Documentation',
            },
            lvl1: ['article h1', '.article h1', 'h1'],
            lvl2: 'article h2',
            lvl3: 'article h3',
            lvl4: 'article h4',
            lvl5: 'article h5',
            lvl6: 'article h6',
            content: ['article p', 'article li', 'article td'],
          },
          indexHeadings: true,
          aggregateContent: true,
          recordVersion: 'v3',
        });

        // --- URL canonicalization: collapse `./page`, `./page/` and
        //     `./page/index.html` to a single canonical form so the same page
        //     cannot be indexed under multiple URLs. ---
        const canonicalize = (u) =>
          String(u || '')
            .replace(/\/index\.html($|[?#])/, '/$1')
            .replace(/\/(#|$|\?)/, '$1');

        // --- Attach a `section` facet derived from the docs path so same-titled
        //     pages across sections can be filtered/ranked apart (e.g. the XAML
        //     vs C# Markup workshop variants). ---
        // `url` is a Location-like object; match against `.pathname` explicitly
        // rather than relying on implicit string coercion.
        const path = (url && url.pathname) || String(url || '');
        const sectionMatch = /\/docs\/articles\/([^/#?]+)/.exec(path);
        const section = sectionMatch ? sectionMatch[1] : 'general';

        return records.map((record) => ({
          ...record,
          url: canonicalize(record.url),
          url_without_anchor: canonicalize(record.url_without_anchor),
          section,
        }));
      },
    },
  ],

  initialIndexSettings: {
    platform: {
      // Group all records by their page URL and keep only the single best one
      // per page — this is the real fix for "one record per heading" duplicates.
      // (Set distinct to 0 temporarily if you want to re-expose sub-headings.)
      attributeForDistinct: 'url_without_anchor',
      distinct: 1,

      attributesForFaceting: [
        'type',
        'lang',
        'language',
        'version',
        'filterOnly(section)',
      ],

      searchableAttributes: [
        'unordered(hierarchy_radio.lvl0)',
        'unordered(hierarchy_radio.lvl1)',
        'unordered(hierarchy_radio.lvl2)',
        'unordered(hierarchy_radio.lvl3)',
        'unordered(hierarchy_radio.lvl4)',
        'unordered(hierarchy_radio.lvl5)',
        'unordered(hierarchy_radio.lvl6)',
        'unordered(hierarchy.lvl0)',
        'unordered(hierarchy.lvl1)',
        'unordered(hierarchy.lvl2)',
        'unordered(hierarchy.lvl3)',
        'unordered(hierarchy.lvl4)',
        'unordered(hierarchy.lvl5)',
        'unordered(hierarchy.lvl6)',
        'content',
      ],

      // pageRank first so curated/important pages (e.g. "Get Started") win
      // intent, then heading level, then position on the page.
      customRanking: [
        'desc(weight.pageRank)',
        'desc(weight.level)',
        'asc(weight.position)',
      ],

      ranking: [
        'words',
        'filters',
        'typo',
        'attribute',
        'proximity',
        'exact',
        'custom',
      ],

      attributesToRetrieve: [
        'hierarchy',
        'content',
        'anchor',
        'url',
        'url_without_anchor',
        'type',
        'section',
      ],
      attributesToHighlight: ['hierarchy', 'content'],
      attributesToSnippet: ['content:10'],
      camelCaseAttributes: ['hierarchy', 'content'],
      highlightPreTag: '<mark>',
      highlightPostTag: '</mark>',
      minWordSizefor1Typo: 3,
      minWordSizefor2Typos: 7,
      allowTyposOnNumericTokens: false,
      minProximity: 1,
      // Curated pages can be boosted with an `optionalWords` / promoted-hit
      // rule in the dashboard; not encoded here.
    },
  },
});
