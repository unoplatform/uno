/**
 * Uno DocSearch client-side result transforms.
 *
 * IMPORTANT (see unoplatform/uno-private#2038):
 *   Deduplication, navigation exclusion and ranking are fundamentally an
 *   *indexing* concern and are handled server-side in the Algolia crawler /
 *   index configuration (see doc/algolia/docsearch-crawler-config.js:
 *   `.remove()` selectors + `attributeForDistinct` / `distinct`).
 *
 *   The transforms below are a *display-layer* stop-gap only. `transformItems`
 *   runs on the already-returned top-N hits per group (bounded by
 *   `maxResultsPerGroup`) and re-runs on every keystroke, so it can only hide /
 *   relabel rows that were already returned — it cannot recover a real result
 *   that a duplicate displaced in the ranking. Keep this logic minimal and let
 *   the crawler config do the real work.
 *
 * This module is intentionally free of DOM/DocSearch dependencies so it can be
 * unit-tested under `node --test` (see docsearch-transform.test.js). It is
 * exposed as `window.unoDocSearch` in the browser and as `module.exports` in
 * Node.
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

    /**
     * Stage 1 — drop crawler breadcrumb-anchor artifacts (`...#breadcrumb`).
     * Interim only: the crawler config also excludes `#breadcrumb` server-side.
     */
    function filterBreadcrumbAnchors(items) {
        return items.filter(function (item) {
            return !(item && item.url && item.url.indexOf('#breadcrumb') !== -1);
        });
    }

    /**
     * Stage 2 — collapse duplicate *page-level* (`lvl1`) records so a page's
     * title appears once, while keeping deeper `lvl2+` heading records so users
     * can still jump to sub-sections. Real per-page dedup is done index-side via
     * `attributeForDistinct: 'url_without_anchor'` + `distinct` — this only tidies
     * the visible dropdown.
     */
    function dedupePageLevel(items) {
        // Object.create(null): no prototype chain, so titles/URLs equal to
        // "__proto__", "constructor", "hasOwnProperty", ... are treated as plain
        // keys and cannot corrupt the map (prototype-pollution safe).
        var seen = Object.create(null);
        return items.filter(function (item) {
            if (!item || !item.url) {
                return true;
            }
            if (item.type === 'lvl1') {
                var base = baseUrl(item.url);
                if (seen[base]) {
                    return false;
                }
                seen[base] = true;
            }
            return true;
        });
    }

    /** "get-started_2" -> "Get Started 2"; drops a trailing .htm(l) file extension. */
    function humanizeSegment(segment) {
        return decodeURIComponent(String(segment))
            .replace(/\.html?$/i, '')
            .replace(/[-_]/g, ' ')
            .replace(/\b\w/g, function (c) { return c.toUpperCase(); })
            .trim();
    }

    function setHighlightValue(result, type, value) {
        var hierarchy = result && result.hierarchy;
        if (hierarchy && hierarchy[type]) {
            hierarchy[type].value = value;
        }
    }

    /**
     * Mutate an item's display title. DocSearch renders from `_snippetResult` /
     * `_highlightResult`, not the raw `hierarchy`, so all three are updated.
     */
    function applyTitle(item, newTitle) {
        if (!item || !item.hierarchy || !item.type) {
            return;
        }
        item.hierarchy[item.type] = newTitle;
        setHighlightValue(item._snippetResult, item.type, newTitle);
        setHighlightValue(item._highlightResult, item.type, newTitle);
    }

    /**
     * Stage 3 — when several results share an identical title but point to
     * different pages (e.g. XAML vs C# Markup workshop variants), append the
     * first URL path segment that distinguishes them so the dropdown rows are
     * telling apart-able.
     *
     * Robust to the "short first URL" edge case: the differing index is chosen
     * treating a missing segment as distinct, and any item lacking a segment at
     * that index falls back to its own last path segment.
     */
    function disambiguateSameTitle(items) {
        var groups = Object.create(null); // prototype-pollution safe (see dedupePageLevel)
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
                    applyTitle(item, title + ' — ' + suffix);
                }
            });
        });

        return items;
    }

    /** Full display transform applied by DocSearch's `transformItems` option. */
    function transformItems(items) {
        if (!items || !items.length) {
            return items || [];
        }
        var result = filterBreadcrumbAnchors(items);
        result = dedupePageLevel(result);
        disambiguateSameTitle(result);
        return result;
    }

    return {
        transformItems: transformItems,
        filterBreadcrumbAnchors: filterBreadcrumbAnchors,
        dedupePageLevel: dedupePageLevel,
        disambiguateSameTitle: disambiguateSameTitle,
        humanizeSegment: humanizeSegment,
        baseUrl: baseUrl,
        pathSegments: pathSegments
    };
});
