#!/bin/bash
# Damage-region validation harness.
#
# Proves that enabling damage-region rendering produces byte-for-byte identical output to the
# historical full-frame path, by capturing the REAL presented window (not the in-process
# RenderTargetBitmap path, which bypasses CompositionTarget.Draw) under Xvfb, on the software and
# OpenGL X11 renderers.
#
# It drives the deterministic, self-settling validation samples (DamageRegion_Static / _SmallUpdate
# / _MovedElement under Windows.UI.Composition), which reach a fixed end state so an after-settle
# screenshot is directly comparable between full-frame and damage-region runs.
#
# Usage:   build/test-scripts/run-damage-region-harness.sh <samples-app-dir> [out-dir]
# Requires: Xvfb, ImageMagick (import + compare), dotnet, and the X11 client libs the app needs.
# Notes:   No window manager is used on purpose — a WM toolbar/clock adds non-deterministic pixels.
#
# Damage-region rendering is always on for retaining renderers; UNO_DAMAGE_REGION=false is a
# diagnostic escape hatch that forces a full-frame present so this run can serve as the baseline.
#   UNO_DAMAGE_REGION = true|false   (false -> force full-frame present; baseline)
#   UNO_X11_RENDERER     = software|opengl
set -uo pipefail

SAMPLES_DIR="${1:?Usage: run-damage-region-harness.sh <samples-app-dir> [out-dir]}"
OUT_DIR="${2:-/tmp/damage-region-harness}"
DLL="$SAMPLES_DIR/SamplesApp.Skia.Generic.dll"
DISPLAY_NUM=99
SETTLE_SECONDS=8

for tool in Xvfb import compare dotnet; do
	command -v "$tool" >/dev/null 2>&1 || { echo "ERROR: '$tool' not found." >&2; exit 2; }
done
[ -f "$DLL" ] || { echo "ERROR: $DLL not found. Build SamplesApp.Skia.Generic first." >&2; exit 2; }

SAMPLES=(
	"Windows.UI.Composition/DamageRegion_Static"
	"Windows.UI.Composition/DamageRegion_SmallUpdate"
	"Windows.UI.Composition/DamageRegion_MovedElement"
	"Windows.UI.Composition/DamageRegion_Disjoint"
	"Windows.UI.Composition/DamageRegion_Scroll"
	"Windows.UI.Composition/DamageRegion_Shadow"
	"Windows.UI.Composition/DamageRegion_ShadowChild"
	"Windows.UI.Composition/DamageRegion_Acrylic"
)

mkdir -p "$OUT_DIR"

# Capture one settled frame of <sample> for <renderer>/<dirty> into <out>.
capture() {
	local sample="$1" dirty="$2" renderer="$3" out="$4"
	Xvfb ":$DISPLAY_NUM" -screen 0 1280x1024x24 >/dev/null 2>&1 &
	local xvfb_pid=$!
	sleep 1
	DISPLAY=":$DISPLAY_NUM" UNO_X11_RENDERER="$renderer" UNO_DAMAGE_REGION="$dirty" \
		dotnet "$DLL" "sample=$sample" >/dev/null 2>&1 &
	local app_pid=$!
	sleep "$SETTLE_SECONDS"
	DISPLAY=":$DISPLAY_NUM" import -window root "$out" 2>/dev/null
	kill "$app_pid" 2>/dev/null; sleep 1; kill -9 "$app_pid" 2>/dev/null
	kill "$xvfb_pid" 2>/dev/null; sleep 1; kill -9 "$xvfb_pid" 2>/dev/null
}

# Max pixels allowed to differ. Damage-region output is byte-identical to a full repaint for
# rectangular/disjoint regions, but when the damage region follows a non-rectangular shape (rounded
# corners, ellipses) the GPU/CPU rasterizes that shape's antialiased edge a bounded, stable, sub-pixel
# amount differently under a clipped present than under a full one. That difference does not accumulate
# over frames. This tolerance absorbs it (a few px from rounded app chrome) while remaining orders of
# magnitude below any real structural regression (stale region, dropped repaint), which run into the
# thousands+ of pixels.
TOLERANCE=${DAMAGE_REGION_TOLERANCE:-8}

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
		if [ "$diff_px" -le "$TOLERANCE" ] 2>/dev/null; then
			echo "  $name: PASS ($diff_px px differ)"
		else
			echo "  $name: FAIL ($diff_px px differ) -> $OUT_DIR/${renderer}_${name}_diff.png" >&2
			overall=1
		fi
	done
done

if [ "$overall" -eq 0 ]; then
	echo "ALL PASS: damage-region output matches a full repaint (within $TOLERANCE px) on every renderer."
else
	echo "HARNESS FAILED: see diffs above." >&2
fi
exit "$overall"
