#!/bin/bash
# Damage-region validation harness for the Skia-WebAssembly (browser) backend.
#
# Proves that damage-region rendering produces output identical to a full-frame repaint on both WASM
# renderers — WebGL (which presents through a retained GPU layer) and Software (a persistent backing bitmap)
# — by driving the real app in a headless browser and comparing a dirty-accumulated frame against a
# full-repaint of the same state (see damage-region-wasm-capture.mjs for how each is produced).
#
# Usage:   build/test-scripts/run-damage-region-harness-wasm.sh <published-wwwroot-dir> [out-dir]
#   <published-wwwroot-dir> is the built web output, e.g.
#     src/SamplesApp/SamplesApp.Skia.WebAssembly.Browser/bin/Debug/net10.0/wwwroot
#
# Prerequisites (one-time):
#   - Build the app:   dotnet build src/SamplesApp/SamplesApp.Skia.WebAssembly.Browser/...  (needs wasm-tools workload)
#   - Node + Playwright + Chromium:   npm i playwright && npx playwright install --with-deps chromium
#   - ImageMagick (compare)
set -uo pipefail

WWWROOT="${1:?Usage: run-damage-region-harness-wasm.sh <published-wwwroot-dir> [out-dir]}"
OUT_DIR="${2:-/tmp/damage-region-harness-wasm}"
PORT=8088
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

for tool in node npx compare; do
	command -v "$tool" >/dev/null 2>&1 || { echo "ERROR: '$tool' not found." >&2; exit 2; }
done
[ -f "$WWWROOT/index.html" ] || { echo "ERROR: $WWWROOT/index.html not found. Build the Skia-WASM app first." >&2; exit 2; }
node -e "require('playwright')" 2>/dev/null || { echo "ERROR: playwright not installed (npm i playwright && npx playwright install chromium)." >&2; exit 2; }

mkdir -p "$OUT_DIR"

# Minimal static file server with correct MIME types for the .NET WASM assets.
SERVER_JS="$OUT_DIR/_server.cjs"
cat > "$SERVER_JS" <<'NODE'
const http = require('http'), fs = require('fs'), path = require('path');
const root = process.argv[2], port = parseInt(process.argv[3], 10);
const mime = { '.html':'text/html','.js':'text/javascript','.mjs':'text/javascript','.css':'text/css',
  '.wasm':'application/wasm','.json':'application/json','.dat':'application/octet-stream',
  '.dll':'application/octet-stream','.pdb':'application/octet-stream','.woff':'font/woff','.woff2':'font/woff2',
  '.ttf':'font/ttf','.png':'image/png','.svg':'image/svg+xml','.ico':'image/x-icon',
  '.blat':'application/octet-stream','.webcil':'application/octet-stream' };
http.createServer((req, res) => {
  let p = decodeURIComponent(req.url.split('?')[0]); if (p === '/') p = '/index.html';
  fs.readFile(path.join(root, p), (err, data) => {
    if (err) { res.writeHead(404); res.end(); return; }
    res.writeHead(200, { 'Content-Type': mime[path.extname(p).toLowerCase()] || 'application/octet-stream' });
    res.end(data);
  });
}).listen(port);
NODE

node "$SERVER_JS" "$WWWROOT" "$PORT" &
SERVER_PID=$!
trap 'kill $SERVER_PID 2>/dev/null' EXIT
sleep 1

overall=0
for renderer in webgl software; do
	a="$OUT_DIR/${renderer}_dirty.png"
	b="$OUT_DIR/${renderer}_full.png"
	if ! node "$SCRIPT_DIR/damage-region-wasm-capture.mjs" "http://localhost:$PORT/" "$renderer" "$a" "$b"; then
		echo "  $renderer: CAPTURE FAILED" >&2; overall=1; continue
	fi
	diff_px=$(compare -metric AE "$a" "$b" "$OUT_DIR/${renderer}_diff.png" 2>&1)
	if [ "$diff_px" = "0" ]; then
		echo "  $renderer: PASS (0 px differ)"
	else
		echo "  $renderer: FAIL ($diff_px px differ) -> $OUT_DIR/${renderer}_diff.png" >&2
		overall=1
	fi
done

if [ "$overall" -eq 0 ]; then
	echo "ALL PASS: Skia-WASM damage-region output is identical to a full repaint on WebGL and Software."
else
	echo "HARNESS FAILED: see diffs above." >&2
fi
exit "$overall"
