// External WASM-Skia accessibility runner.
//
// Loads a served Uno WASM-Skia app in headless Chromium (Playwright), enables the accessibility
// semantic overlay via the on-screen "Enable accessibility" button (once the app has rendered),
// optionally drives an interaction, then serializes the ARIA semantic tree under
// #uno-semantics-root to JSON.
//
// Why Playwright and not the C# Appium harness: the WASM-Skia accessibility tree is the semantic
// DOM overlay (#uno-semantics-root, role/aria-*/xamlautomationid, id="uno-semantics-{handle}"),
// which any DOM-capable WebDriver can read. Playwright bundles its own Chromium, so this runs on a
// machine with no system browser. To drive it from the existing Appium chromium driver instead,
// reuse the serializeInPage() body via execute_script.
//
// Usage:
//   npm install --no-save playwright && npx playwright install chromium
//   python3 -m http.server 8080 --directory <app>/bin/Release/<tfm>-browserwasm/wwwroot &
//   URL=http://localhost:8080/ node dump-semantics.cjs
//
// Env: URL, ACTION ("sort-name"|"select-row"), OUT (json path), WAIT_MS (grid poll budget).
const { chromium } = require('playwright');
const fs = require('fs');

const URL = process.env.URL || 'http://localhost:8080/';
const ACTION = process.env.ACTION || '';
const OUT = process.env.OUT || '';
const WAIT_MS = parseInt(process.env.WAIT_MS || '120000', 10);

function serializeInPage() {
  const KEYS = (el) =>
    Array.from(el.attributes)
      .filter(a => a.name.startsWith('aria-') || a.name === 'role' ||
        a.name === 'xamlautomationid' || a.name === 'tabindex')
      .reduce((o, a) => { o[a.name] = a.value; return o; }, {});

  function walk(el, depth) {
    if (!el || depth > 40) return null;
    const node = { tag: el.tagName.toLowerCase(), id: el.id || undefined, attrs: KEYS(el) };
    const directText = Array.from(el.childNodes)
      .filter(n => n.nodeType === 3).map(n => n.textContent.trim()).join(' ').trim();
    if (directText) node.text = directText.slice(0, 60);
    const kids = [];
    for (const c of el.children) { const k = walk(c, depth + 1); if (k) kids.push(k); }
    if (kids.length) node.children = kids;
    return node;
  }

  const root = document.getElementById('uno-semantics-root');
  const roleHist = {};
  if (root) root.querySelectorAll('[role]').forEach(e => {
    const r = e.getAttribute('role'); roleHist[r] = (roleHist[r] || 0) + 1;
  });

  const report = {};
  for (const role of ['grid', 'row', 'columnheader', 'gridcell']) {
    const els = root ? Array.from(root.querySelectorAll(`[role="${role}"]`)) : [];
    const attrKeys = new Set();
    els.forEach(e => Object.keys(KEYS(e)).forEach(k => attrKeys.add(k)));
    report[role] = {
      count: els.length,
      attrKeys: Array.from(attrKeys).sort(),
      samples: els.slice(0, 6).map(e => KEYS(e)),
    };
  }
  return {
    rootExists: !!root,
    rootDescendantCount: root ? root.querySelectorAll('*').length : 0,
    roleHist, report,
    tree: root ? walk(root, 0) : null,
  };
}

async function enableAccessibility(page) {
  await page.waitForSelector('#uno-enable-accessibility', { state: 'attached', timeout: 180000 });
  // Give the visual tree time to attach so EnableAccessibility() sees a ready Window. The runtime's
  // own retry budget is ~2s, which is too short for an interpreter-mode WASM cold start.
  await page.waitForTimeout(5000);
  await page.evaluate(() => {
    const b = document.getElementById('uno-enable-accessibility');
    if (b) b.click();
  });
}

(async () => {
  const browser = await chromium.launch({ args: ['--no-sandbox'] });
  const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });
  page.setDefaultTimeout(180000);
  const errors = [];
  page.on('pageerror', e => errors.push(String(e)));

  await page.goto(URL, { waitUntil: 'domcontentloaded' });
  await enableAccessibility(page);

  const deadline = Date.now() + WAIT_MS;
  let gridSeen = false;
  while (Date.now() < deadline) {
    gridSeen = await page.evaluate(() => !!document.querySelector('#uno-semantics-root [role="grid"]'));
    if (gridSeen) break;
    await page.waitForTimeout(1000);
  }
  await page.waitForTimeout(1500);

  let action = null;
  if (gridSeen && ACTION === 'sort-name') {
    const before = await page.evaluate(() => { const h = document.querySelector('#uno-semantics-root [role="columnheader"]'); return h ? h.getAttribute('aria-sort') : null; });
    await page.evaluate(() => { const h = document.querySelector('#uno-semantics-root [role="columnheader"]'); if (h) h.click(); });
    await page.waitForTimeout(1500);
    const after = await page.evaluate(() => { const h = document.querySelector('#uno-semantics-root [role="columnheader"]'); return h ? h.getAttribute('aria-sort') : null; });
    action = { kind: 'sort-name', ariaSortBefore: before, ariaSortAfter: after };
  } else if (gridSeen && ACTION === 'select-row') {
    const before = await page.evaluate(() => { const r = document.querySelector('#uno-semantics-root [role="row"]'); return r ? r.getAttribute('aria-selected') : null; });
    await page.evaluate(() => { const c = document.querySelector('#uno-semantics-root [role="gridcell"]'); if (c) c.click(); });
    await page.waitForTimeout(1500);
    const after = await page.evaluate(() => { const r = document.querySelector('#uno-semantics-root [role="row"]'); return r ? r.getAttribute('aria-selected') : null; });
    action = { kind: 'select-row', ariaSelectedBefore: before, ariaSelectedAfter: after };
  }

  const result = await page.evaluate(serializeInPage);
  result.url = URL;
  result.gridSeen = gridSeen;
  result.pageErrors = errors;
  if (action) result.action = action;

  const json = JSON.stringify(result, null, 2);
  console.log(json);
  if (OUT) fs.writeFileSync(OUT, json);
  await browser.close();
})().catch(e => { console.error('DUMP_FAILED', e); process.exit(1); });
