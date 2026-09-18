/**
 * Unit tests for docsearch-transform.js — run with:  node --test
 * (from doc/templates/uno/service/, or `node --test doc/templates/uno/service`)
 *
 * Breadcrumb filtering and page-level dedup are now handled server-side (Algolia
 * crawler / index config, see unoplatform/uno-private#2038), so the only client
 * transform left is same-title disambiguation. Covered here including the
 * short-first-URL edge case, <mark> highlight preservation, and prototype-key
 * inputs (__proto__/constructor).
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

test('same title on different pages gets a distinguishing suffix', () => {
    const a = hit('lvl1', 'Get Started', BASE + '/xaml/get-started.html');
    const b = hit('lvl1', 'Get Started', BASE + '/csharp/get-started.html');
    t.disambiguateSameTitle([a, b]);
    assert.strictEqual(a.hierarchy.lvl1, 'Get Started — Xaml');
    assert.strictEqual(b.hierarchy.lvl1, 'Get Started — Csharp');
    // DocSearch renders these two — both must be updated:
    assert.strictEqual(a._snippetResult.hierarchy.lvl1.value, 'Get Started — Xaml');
    assert.strictEqual(a._highlightResult.hierarchy.lvl1.value, 'Get Started — Xaml');
});

test('disambiguation preserves existing <mark> highlight markup', () => {
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

test('identical titles on the SAME page are left untouched', () => {
    const a = hit('lvl1', 'Same', BASE + '/same.html');
    const b = hit('lvl1', 'Same', BASE + '/same.html');
    t.disambiguateSameTitle([a, b]);
    assert.strictEqual(a.hierarchy.lvl1, 'Same');
    assert.strictEqual(b.hierarchy.lvl1, 'Same');
});

test('unique titles are left untouched', () => {
    const a = hit('lvl1', 'Alpha', BASE + '/a.html');
    t.disambiguateSameTitle([a]);
    assert.strictEqual(a.hierarchy.lvl1, 'Alpha');
});

test('short-first-URL edge case still disambiguates every item', () => {
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

test('transformItems: disambiguates without removing records (dedup is server-side)', () => {
    const items = [
        hit('lvl1', 'Get Started', BASE + '/xaml/get-started.html'),
        hit('lvl1', 'Get Started', BASE + '/csharp/get-started.html'),
        hit('lvl1', 'MVUX', BASE + '/mvux.html') // unique title -> untouched
    ];
    const out = t.transformItems(items);
    assert.strictEqual(out.length, 3, 'no records removed — dedup/exclusion are handled server-side now');
    const gs = out.filter(i => i.hierarchy.lvl1.indexOf('Get Started') === 0);
    assert.deepStrictEqual(gs.map(i => i.hierarchy.lvl1).sort(), ['Get Started — Csharp', 'Get Started — Xaml']);
    assert.strictEqual(out.find(i => t.baseUrl(i.url) === BASE + '/mvux.html').hierarchy.lvl1, 'MVUX');
});

test('transformItems: empty / nullish input is handled', () => {
    assert.deepStrictEqual(t.transformItems([]), []);
    assert.deepStrictEqual(t.transformItems(null), []);
    assert.deepStrictEqual(t.transformItems(undefined), []);
});
