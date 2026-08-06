/**
 * Unit tests for docsearch-transform.js — run with:  node --test
 * (from doc/templates/uno/service/, or `node --test doc/templates/uno/service`)
 *
 * Covers the cases called out in unoplatform/uno-private#2038:
 * breadcrumb filtering, page-level dedup, same-title disambiguation (including
 * the short-first-URL edge case), and prototype-key inputs (__proto__/constructor).
 */
'use strict';

const test = require('node:test');
const assert = require('node:assert');
const t = require('./docsearch-transform.js');

const BASE = 'https://platform.uno/docs/articles';

/** Minimal DocSearch-hit factory. */
function hit(type, title, url, extra) {
    const h = {
        type: type,
        url: url,
        hierarchy: { lvl0: 'Docs' },
        _snippetResult: { hierarchy: {} },
        _highlightResult: { hierarchy: {} }
    };
    h.hierarchy[type] = title;
    h._snippetResult.hierarchy[type] = { value: title };
    h._highlightResult.hierarchy[type] = { value: title };
    return Object.assign(h, extra || {});
}

test('baseUrl strips fragment and query', () => {
    assert.strictEqual(t.baseUrl(BASE + '/a.html?q=2'), BASE + '/a.html');
    assert.strictEqual(t.baseUrl(BASE + '/a.html#frag'), BASE + '/a.html');
    assert.strictEqual(t.baseUrl(BASE + '/a.html#frag?q=2'), BASE + '/a.html');
    assert.strictEqual(t.baseUrl(null), '');
    assert.strictEqual(t.baseUrl(undefined), '');
});

test('humanizeSegment: strips .html, separators, title-cases', () => {
    assert.strictEqual(t.humanizeSegment('get-started.html'), 'Get Started');
    assert.strictEqual(t.humanizeSegment('c_markup'), 'C Markup');
    assert.strictEqual(t.humanizeSegment('xaml'), 'Xaml');
    assert.strictEqual(t.humanizeSegment('hot%20design'), 'Hot Design');
});

test('Stage 1: breadcrumb-anchored records are removed', () => {
    const items = [
        hit('lvl1', 'Page', BASE + '/page.html'),
        hit('lvl2', 'Crumb', BASE + '/page.html#breadcrumb'),
        hit('lvl2', 'Real', BASE + '/page.html#section')
    ];
    const out = t.filterBreadcrumbAnchors(items);
    assert.strictEqual(out.length, 2);
    assert.ok(!out.some(i => i.url.includes('#breadcrumb')));
});

test('Stage 2: duplicate lvl1 records for the same page collapse to one', () => {
    const items = [
        hit('lvl1', 'MVUX', BASE + '/mvux.html'),
        hit('lvl1', 'MVUX', BASE + '/mvux.html#top'), // same base url
        hit('lvl2', 'Feeds', BASE + '/mvux.html#feeds'),
        hit('lvl2', 'States', BASE + '/mvux.html#states'),
        hit('lvl1', 'Other', BASE + '/other.html')
    ];
    const out = t.dedupePageLevel(items);
    const lvl1 = out.filter(i => i.type === 'lvl1');
    assert.strictEqual(lvl1.filter(i => t.baseUrl(i.url) === BASE + '/mvux.html').length, 1,
        'only one lvl1 per base URL');
    // lvl2 sub-sections are intentionally preserved
    assert.strictEqual(out.filter(i => i.type === 'lvl2').length, 2);
    assert.ok(out.some(i => t.baseUrl(i.url) === BASE + '/other.html'));
});

test('Stage 2: records without a url are kept', () => {
    const noUrl = hit('lvl1', 'NoUrl', undefined);
    noUrl.url = undefined;
    const out = t.dedupePageLevel([noUrl, hit('lvl1', 'P', BASE + '/p.html')]);
    assert.strictEqual(out.length, 2);
});

test('Stage 3: same title on different pages gets a distinguishing suffix', () => {
    const a = hit('lvl1', 'Get Started', BASE + '/xaml/get-started.html');
    const b = hit('lvl1', 'Get Started', BASE + '/csharp/get-started.html');
    t.disambiguateSameTitle([a, b]);
    assert.strictEqual(a.hierarchy.lvl1, 'Get Started — Xaml');
    assert.strictEqual(b.hierarchy.lvl1, 'Get Started — Csharp');
    // DocSearch renders these two — both must be updated:
    assert.strictEqual(a._snippetResult.hierarchy.lvl1.value, 'Get Started — Xaml');
    assert.strictEqual(a._highlightResult.hierarchy.lvl1.value, 'Get Started — Xaml');
});

test('Stage 3: disambiguation preserves existing <mark> highlight markup', () => {
    function marked(title, url) {
        var h = hit('lvl1', title, url);
        h._highlightResult.hierarchy.lvl1.value = 'Get <mark>Started</mark>';
        h._snippetResult.hierarchy.lvl1.value = 'Get <mark>Started</mark>';
        return h;
    }
    var a = marked('Get Started', BASE + '/xaml/get-started.html');
    var b = marked('Get Started', BASE + '/csharp/get-started.html');
    t.disambiguateSameTitle([a, b]);
    assert.strictEqual(a.hierarchy.lvl1, 'Get Started — Xaml'); // raw hierarchy still gets the suffix
    // highlight/snippet keep the <mark> markup, with the suffix appended (not overwritten):
    assert.strictEqual(a._highlightResult.hierarchy.lvl1.value, 'Get <mark>Started</mark> — Xaml');
    assert.strictEqual(a._snippetResult.hierarchy.lvl1.value, 'Get <mark>Started</mark> — Xaml');
    assert.strictEqual(b._highlightResult.hierarchy.lvl1.value, 'Get <mark>Started</mark> — Csharp');
});

test('Stage 3: identical titles on the SAME page are left untouched', () => {
    const a = hit('lvl1', 'Same', BASE + '/same.html');
    const b = hit('lvl1', 'Same', BASE + '/same.html');
    t.disambiguateSameTitle([a, b]);
    assert.strictEqual(a.hierarchy.lvl1, 'Same');
    assert.strictEqual(b.hierarchy.lvl1, 'Same');
});

test('Stage 3: unique titles are left untouched', () => {
    const a = hit('lvl1', 'Alpha', BASE + '/a.html');
    t.disambiguateSameTitle([a]);
    assert.strictEqual(a.hierarchy.lvl1, 'Alpha');
});

test('Stage 3: short-first-URL edge case still disambiguates every item', () => {
    // First URL is shorter than the others (no segment at the differing index).
    const short = hit('lvl1', 'Overview', BASE + '/overview.html');
    const deepA = hit('lvl1', 'Overview', BASE + '/mvux/overview.html');
    const deepB = hit('lvl1', 'Overview', BASE + '/nav/overview.html');
    t.disambiguateSameTitle([short, deepA, deepB]);
    // Every row must end up with a distinct, non-empty suffix (no bare "Overview").
    const titles = [short, deepA, deepB].map(i => i.hierarchy.lvl1);
    titles.forEach(title => assert.ok(/ — .+/.test(title), 'has a suffix: ' + title));
    assert.strictEqual(new Set(titles).size, 3, 'all three titles are distinct: ' + titles.join(' | '));
});

test('prototype-pollution: titles of "__proto__" / "constructor" do not throw or corrupt', () => {
    // Two pages both titled "constructor" — the pre-refactor code threw here
    // because titleGroups["constructor"] resolved to Object.prototype.constructor.
    const c1 = hit('lvl1', 'constructor', BASE + '/xaml/ctor.html');
    const c2 = hit('lvl1', 'constructor', BASE + '/csharp/ctor.html');
    const p1 = hit('lvl1', '__proto__', BASE + '/a/proto.html');
    const p2 = hit('lvl1', '__proto__', BASE + '/b/proto.html');

    assert.doesNotThrow(() => t.disambiguateSameTitle([c1, c2, p1, p2]));
    assert.strictEqual(c1.hierarchy.lvl1, 'constructor — Xaml');
    assert.strictEqual(c2.hierarchy.lvl1, 'constructor — Csharp');
    assert.strictEqual(p1.hierarchy.lvl1, '__proto__ — A');
    assert.strictEqual(p2.hierarchy.lvl1, '__proto__ — B');

    // No global prototype corruption occurred.
    assert.strictEqual(({}).polluted, undefined);
    assert.ok(Array.isArray([]));
});

test('prototype-pollution: dedup keys of "__proto__" / "constructor" are safe', () => {
    const items = [
        hit('lvl1', 'A', 'constructor'),
        hit('lvl1', 'A', 'constructor'),   // same "url" -> deduped
        hit('lvl1', 'B', '__proto__'),
        hit('lvl1', 'B', '__proto__')      // same "url" -> deduped
    ];
    let out;
    assert.doesNotThrow(() => { out = t.dedupePageLevel(items); });
    assert.strictEqual(out.length, 2);
});

test('transformItems: end-to-end pipeline', () => {
    const items = [
        hit('lvl1', 'MVUX', BASE + '/mvux.html'),
        hit('lvl1', 'MVUX', BASE + '/mvux.html#dup'),          // deduped
        hit('lvl2', 'Crumb', BASE + '/mvux.html#breadcrumb'),  // filtered
        hit('lvl1', 'Get Started', BASE + '/xaml/get-started.html'),
        hit('lvl1', 'Get Started', BASE + '/csharp/get-started.html')
    ];
    const out = t.transformItems(items);
    assert.ok(!out.some(i => i.url.includes('#breadcrumb')), 'breadcrumb filtered');
    assert.strictEqual(out.filter(i => t.baseUrl(i.url) === BASE + '/mvux.html' && i.type === 'lvl1').length, 1);
    const gs = out.filter(i => i.hierarchy.lvl1 && i.hierarchy.lvl1.indexOf('Get Started') === 0);
    assert.deepStrictEqual(gs.map(i => i.hierarchy.lvl1).sort(), ['Get Started — Csharp', 'Get Started — Xaml']);
});

test('transformItems: empty / nullish input is handled', () => {
    assert.deepStrictEqual(t.transformItems([]), []);
    assert.deepStrictEqual(t.transformItems(null), []);
    assert.deepStrictEqual(t.transformItems(undefined), []);
});
