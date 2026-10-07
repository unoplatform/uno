#!/bin/bash
# Prints the WebGPU adapter Chrome hands a page under several flag sets, so a lane can tell a hardware
# adapter from the SwiftShader fallback, and which flags unlock the former. Diagnostic only: never fails.
set -u

CHROME=${CHROME:-"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"}
PAGE_DIR="$(cd "$(dirname "$0")/webgpu-probe" && pwd)"
PYTHON=$(command -v python3 || command -v python)

"$PYTHON" -m http.server 8765 -d "$PAGE_DIR" >/dev/null 2>&1 &
SERVER=$!
sleep 2

probe() {
    local profile log pid
    profile=$(mktemp -d)
    log=$(mktemp)
    echo "== flags: $*"
    "$CHROME" --user-data-dir="$profile" --no-first-run --no-default-browser-check --disable-search-engine-choice-screen \
        --enable-logging=stderr --v=0 "$@" http://localhost:8765/index.html > "$log" 2>&1 &
    pid=$!
    for _ in $(seq 1 30); do
        grep -q "PROBE" "$log" && break
        sleep 1
    done
    grep -o '"PROBE [^"]*"' "$log" || echo "   (no adapter logged within 30s)"
    grep -iE "blocklist|webgpu.*(disabled|unavailable)|adapter" "$log" | grep -v PROBE | cut -c1-200 | head -5
    kill "$pid" 2>/dev/null
    sleep 2
    pkill -f "$profile" 2>/dev/null
    rm -rf "$profile" "$log"
}

probe --enable-unsafe-webgpu
probe --enable-unsafe-webgpu --ignore-gpu-blocklist
probe --enable-unsafe-webgpu --ignore-gpu-blocklist --disable-software-rasterizer
probe --enable-unsafe-webgpu --ignore-gpu-blocklist --use-angle=metal
probe --enable-unsafe-webgpu --ignore-gpu-blocklist --enable-features=SkiaGraphite

kill "$SERVER" 2>/dev/null
exit 0
