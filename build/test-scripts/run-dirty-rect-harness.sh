#!/bin/bash
# Dirty-rectangles validation harness.
#
# Proves that enabling dirty-rectangles rendering produces byte-for-byte identical output to the
# historical full-frame path, by capturing the REAL presented window (not the in-process
# RenderTargetBitmap path, which bypasses CompositionTarget.Draw) under Xvfb, on the software and
# OpenGL X11 renderers.
#
# It drives the deterministic, self-settling validation samples (DirtyRectangles_Static / _SmallUpdate
# / _MovedElement under Windows.UI.Composition), which reach a fixed end state so an after-settle
# screenshot is directly comparable between full-frame and dirty-rectangles runs.
#
# Usage:   build/test-scripts/run-dirty-rect-harness.sh <samples-app-dir> [out-dir]
# Requires: Xvfb, ImageMagick (import + compare), dotnet, and the X11 client libs the app needs.
# Notes:   No window manager is used on purpose — a WM toolbar/clock adds non-deterministic pixels.
#
# Dirty-rectangles rendering is always on for retaining renderers; UNO_DIRTY_RECTANGLES=false is a
# diagnostic escape hatch that forces a full-frame present so this run can serve as the baseline.
#   UNO_DIRTY_RECTANGLES = true|false   (false -> force full-frame present; baseline)
#   UNO_X11_RENDERER     = software|opengl
set -uo pipefail

SAMPLES_DIR="${1:?Usage: run-dirty-rect-harness.sh <samples-app-dir> [out-dir]}"
OUT_DIR="${2:-/tmp/dirty-rect-harness}"
DLL="$SAMPLES_DIR/SamplesApp.Skia.Generic.dll"
DISPLAY_NUM=99
SETTLE_SECONDS=8

for tool in Xvfb import compare dotnet; do
	command -v "$tool" >/dev/null 2>&1 || { echo "ERROR: '$tool' not found." >&2; exit 2; }
done
[ -f "$DLL" ] || { echo "ERROR: $DLL not found. Build SamplesApp.Skia.Generic first." >&2; exit 2; }

SAMPLES=(
	"Windows.UI.Composition/DirtyRectangles_Static"
	"Windows.UI.Composition/DirtyRectangles_SmallUpdate"
	"Windows.UI.Composition/DirtyRectangles_MovedElement"
	"Windows.UI.Composition/DirtyRectangles_Disjoint"
	"Windows.UI.Composition/DirtyRectangles_Scroll"
	"Windows.UI.Composition/DirtyRectangles_Shadow"
	"Windows.UI.Composition/DirtyRectangles_ShadowChild"
	"Windows.UI.Composition/DirtyRectangles_Acrylic"
)

mkdir -p "$OUT_DIR"

# Capture one settled frame of <sample> for <renderer>/<dirty> into <out>.
capture() {
	local sample="$1" dirty="$2" renderer="$3" out="$4"
	Xvfb ":$DISPLAY_NUM" -screen 0 1280x1024x24 >/dev/null 2>&1 &
	local xvfb_pid=$!
	sleep 1
	DISPLAY=":$DISPLAY_NUM" UNO_X11_RENDERER="$renderer" UNO_DIRTY_RECTANGLES="$dirty" \
		dotnet "$DLL" "sample=$sample" >/dev/null 2>&1 &
	local app_pid=$!
	sleep "$SETTLE_SECONDS"
	DISPLAY=":$DISPLAY_NUM" import -window root "$out" 2>/dev/null
	kill "$app_pid" 2>/dev/null; sleep 1; kill -9 "$app_pid" 2>/dev/null
	kill "$xvfb_pid" 2>/dev/null; sleep 1; kill -9 "$xvfb_pid" 2>/dev/null
}

overall=0
for renderer in software opengl; do
	echo "=== Renderer: $renderer ==="
	for sample in "${SAMPLES[@]}"; do
		name="${sample##*/}"
		off="$OUT_DIR/${renderer}_${name}_off.png"
		on="$OUT_DIR/${renderer}_${name}_on.png"
		capture "$sample" false "$renderer" "$off"
		capture "$sample" true  "$renderer" "$on"
		if [ ! -s "$off" ] || [ ! -s "$on" ]; then
			echo "  $name: CAPTURE FAILED" >&2; overall=1; continue
		fi
		diff_px=$(compare -metric AE "$off" "$on" "$OUT_DIR/${renderer}_${name}_diff.png" 2>&1)
		if [ "$diff_px" = "0" ]; then
			echo "  $name: PASS (0 px differ)"
		else
			echo "  $name: FAIL ($diff_px px differ) -> $OUT_DIR/${renderer}_${name}_diff.png" >&2
			overall=1
		fi
	done
done

if [ "$overall" -eq 0 ]; then
	echo "ALL PASS: dirty-rectangles output is byte-identical to full-frame on every renderer."
else
	echo "HARNESS FAILED: see diffs above." >&2
fi
exit "$overall"
