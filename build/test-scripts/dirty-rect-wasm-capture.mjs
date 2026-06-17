// Playwright capture helper for the Skia-WebAssembly dirty-rectangles harness.
//
// Loads the served Skia-WASM SamplesApp in a headless browser, scrolls the category list to generate a
// stream of dirty-rectangle updates (the whole viewport shifts without each item repainting), then captures
// two screenshots of the same settled state:
//   A) the dirty-rectangles-accumulated frame, and
//   B) a full repaint, forced by a 1px viewport resize round-trip (a size change makes CompositionTarget.Draw
//      treat the surface as recreated and present the whole frame unclipped).
// If dirty-rectangles rendering is correct, A and B are pixel-identical. The bash harness diffs them.
//
// Usage: node dirty-rect-wasm-capture.mjs <url> <renderer: webgl|software> <outA.png> <outB.png>
import { chromium } from 'playwright';

const [url, renderer, outA, outB] = process.argv.slice(2);
if (!url || !renderer || !outA || !outB) {
	console.error('Usage: node dirty-rect-wasm-capture.mjs <url> <webgl|software> <outA.png> <outB.png>');
	process.exit(2);
}

const browser = await chromium.launch({ args: ['--ignore-gpu-blocklist', '--use-gl=swiftshader'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 1024 } });

if (renderer === 'software') {
	// Make WebGL unavailable so BrowserRenderer falls back to SoftwareBrowserRenderer.
	await page.addInitScript(() => {
		const orig = HTMLCanvasElement.prototype.getContext;
		HTMLCanvasElement.prototype.getContext = function (type, ...rest) {
			if (typeof type === 'string' && type.toLowerCase().includes('webgl')) {
				return null;
			}
			return orig.call(this, type, ...rest);
		};
	});
}

await page.goto(url, { waitUntil: 'load', timeout: 60000 });
await page.waitForSelector('canvas', { timeout: 120000 });
await page.waitForTimeout(8000); // initial full render + settle

// Scroll the left category list to produce many dirty-rectangle frames.
await page.mouse.move(140, 400);
for (let i = 0; i < 12; i++) {
	await page.mouse.wheel(0, 220);
	await page.waitForTimeout(60);
}
await page.mouse.move(660, 500); // neutral spot: no hover highlight
await page.waitForTimeout(4000);  // let the auto-hiding scrollbar fully fade (timing-independent capture)

await page.screenshot({ path: outA });

// Force a full repaint via a resize round-trip, then capture the same state.
await page.setViewportSize({ width: 1280, height: 1025 });
await page.waitForTimeout(700);
await page.setViewportSize({ width: 1280, height: 1024 });
await page.mouse.move(660, 500);
await page.waitForTimeout(4000);

await page.screenshot({ path: outB });

await browser.close();
console.log(`captured ${renderer}: ${outA} (dirty) and ${outB} (full)`);
