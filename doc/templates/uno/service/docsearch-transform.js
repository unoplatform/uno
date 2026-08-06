/**
 * Uno DocSearch client-side result transform.
 *
 * IMPORTANT (see unoplatform/uno-private#2038):
 *   Deduplication and navigation exclusion are handled SERVER-SIDE in the Algolia
 *   crawler / index config (see doc/algolia/docsearch-crawler-config.js): chrome
 *   is stripped via `$(...).remove()` and each page collapses to one result via
 *   `attributeForDistinct: 'url_without_anchor'` + `distinct`. Those are now live,
 *   so the former client-side breadcrumb filter and page-level dedup have been
 *   removed.
 *
 *   The one thing left client-side is same-title disambiguation: when several
 *   results share an identical title but point to different pages, append a
 *   distinguishing URL path segment so the dropdown rows are tellable apart.
 *   (`transformItems` only sees the already-returned top-N hits per group; the
 *   index-side equivalent is the optional `section` facet in the crawler config.)
 *
 * This module is free of DOM/DocSearch dependencies so it can be unit-tested
 * under `node --test` (see docsearch-transform.test.js). It is exposed as
 * `window.unoDocSearch` in the browser and `module.exports` in Node.
 */
(function (root, factory) {
    'use strict';
    var api = factory();
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = api;
    }
    if (root) {
        root.unoDocSearch = api;
    }
})(typeof self !== 'undefined' ? self : (typeof globalThis !== 'undefined' ? globalThis : this), function () {
    'use strict';

    /** Strip the fragment and query string from a URL, returning the base page URL. */
    function baseUrl(url) {
        return String(url == null ? '' : url).split('#')[0].split('?')[0];
    }

    /** Split a URL into non-empty path segments (host/protocol segments included). */
    function pathSegments(url) {
        return baseUrl(url).split('/').filter(Boolean);
    }

    /** "get-started_2" -> "Get Started 2"; drops a trailing .htm(l) file extension. */
    function humanizeSegment(segment) {
        return decodeURIComponent(String(segment))
            .replace(/\.html?$/i, '')
            .replace(/[-_]/g, ' ')
            .replace(/\b\w/g, function (c) { return c.toUpperCase(); })
            .trim();
    }

    function appendToHighlight(result, type, sep) {
        var hierarchy = result && result.hierarchy;
        if (hierarchy && hierarchy[type] && typeof hierarchy[type].value === 'string') {
            // DocSearch renders these values as HTML and they may contain <mark>
            // highlight markup, so append the suffix rather than overwriting —
            // overwriting with a plain string would strip the highlighting.
            hierarchy[type].value = hierarchy[type].value + sep;
        }
    }

    /**
     * Append a disambiguating suffix (" — <suffix>") to an item's display title.
     * DocSearch renders from `_snippetResult` / `_highlightResult`, not the raw
     * `hierarchy`, so all three are updated; the highlight/snippet values keep
     * their existing markup.
     */
    function appendSuffix(item, suffix) {
        if (!item || !item.hierarchy || !item.type || !suffix) {
            return;
        }
        var type = item.type;
        var sep = ' — ' + suffix;
        var current = item.hierarchy[type];
        item.hierarchy[type] = (current == null ? '' : String(current)) + sep;
        appendToHighlight(item._snippetResult, type, sep);
        appendToHighlight(item._highlightResult, type, sep);
    }

    /**
     * When several results share an identical title but point to different pages
     * (e.g. XAML vs C# Markup workshop variants), append the first URL path
     * segment that distinguishes them so the dropdown rows are tellable apart.
     *
     * Robust to the "short first URL" edge case: the differing index is chosen
     * treating a missing segment as distinct, and any item lacking a segment at
     * that index falls back to its own last path segment.
     */
    function disambiguateSameTitle(items) {
        // Object.create(null): no prototype chain, so titles equal to "__proto__",
        // "constructor", "hasOwnProperty", … are treated as plain keys and cannot
        // corrupt the map (prototype-pollution safe).
        var groups = Object.create(null);
        items.forEach(function (item) {
            var hierarchy = item && item.hierarchy;
            var title = (hierarchy && item.type && hierarchy[item.type]) || '';
            if (title) {
                (groups[title] || (groups[title] = [])).push(item);
            }
        });

        Object.keys(groups).forEach(function (title) {
            var group = groups[title];
            if (group.length < 2) {
                return;
            }
            var segArrays = group.map(function (item) { return pathSegments(item.url); });
            var maxLen = segArrays.reduce(function (max, segs) { return Math.max(max, segs.length); }, 0);

            var diffIdx = -1;
            for (var s = 0; s < maxLen && diffIdx < 0; s++) {
                var first = segArrays[0][s];
                for (var g = 1; g < segArrays.length; g++) {
                    // `!==` treats a missing segment (undefined) as distinct.
                    if (segArrays[g][s] !== first) {
                        diffIdx = s;
                        break;
                    }
                }
            }
            if (diffIdx < 0) {
                return; // identical paths — nothing to disambiguate with
            }

            group.forEach(function (item, gi) {
                var segs = segArrays[gi];
                var seg = segs[diffIdx];
                if (!seg) {
                    // Short URL: no segment at the differing index. Fall back to
                    // this item's own last segment so it still gets a suffix.
                    seg = segs.length ? segs[segs.length - 1] : '';
                }
                var suffix = seg ? humanizeSegment(seg) : '';
                if (suffix) {
                    appendSuffix(item, suffix);
                }
            });
        });

        return items;
    }

    /** Display transform applied by DocSearch's `transformItems` option. */
    function transformItems(items) {
        if (!items || !items.length) {
            return items || [];
        }
        disambiguateSameTitle(items);
        return items;
    }

    return {
        transformItems: transformItems,
        disambiguateSameTitle: disambiguateSameTitle,
        humanizeSegment: humanizeSegment,
        baseUrl: baseUrl,
        pathSegments: pathSegments
    };
});
