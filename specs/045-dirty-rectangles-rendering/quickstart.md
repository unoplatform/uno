# Quickstart: Headless validation harness for Dirty Rectangles

This is the **first thing to build** (per the feature directive): a reproducible headless run of `SamplesApp.Skia.Generic` under xvfb, on both the **software** and **hardware (OpenGL)** X11 renderers, that proves visual output is **identical before and after** the dirty-rectangles change.

## Prerequisites

- Linux with `xvfb` and a lightweight WM (`fluxbox`) — same setup CI uses (`build/test-scripts/linux-skia-runtime-tests.sh`).
  ```bash
  sudo apt-get install -y xvfb fluxbox
  ```
- Cross-targeting override set to a single Skia TFM (see AGENTS.md). For fast iteration:
  ```bash
  cd src && cp crosstargeting_override.props.sample crosstargeting_override.props
  # set <UnoTargetFrameworkOverride>net10.0</UnoTargetFrameworkOverride> and <UnoFastDevBuild>true</UnoFastDevBuild>
  ```

## Build

```bash
cd /workspace/uno
dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj \
  -c Release -p:UnoTargetFrameworkOverride=net10.0 -f net10.0
# output: src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0/SamplesApp.Skia.Generic.dll
```

## Run a scene headless (software renderer)

```bash
DLL=src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0/SamplesApp.Skia.Generic.dll
xvfb-run --auto-servernum --server-args='-screen 0 1280x1024x24' \
  sh -c "fluxbox >/dev/null 2>&1 & dotnet $DLL --auto-screenshots=/tmp/dr-sw-off"
```

- Select renderer: **software** via `FeatureConfiguration.Rendering.UseOpenGLOnX11 = false` (or `X11HostBuilder.RenderingBackend(X11RenderingBackend.Software)`); **hardware** via `UseOpenGLOnX11 = true` (`…OpenGL`). The harness sets this per run (env var / launch arg wired during implementation).
- Toggle the feature: `FeatureConfiguration.Rendering.EnableDirtyRectangles = true|false`.
- Focus a single scene instead of all: append `"sample=<Category>/<SampleName>"` (e.g. `"sample=Windows_UI_Xaml_Controls/Button"`).
- Screenshots land under the `--auto-screenshots` dir (`UITests-<timestamp>/*.png`).

## The before/after equality gate

The harness script (`build/test-scripts/run-dirty-rect-harness.sh`, added during implementation) runs each scene set twice per renderer and compares:

```text
for renderer in software opengl:
    capture  EnableDirtyRectangles=false  → /tmp/dr-<renderer>-off
    capture  EnableDirtyRectangles=true   → /tmp/dr-<renderer>-on
    assert   pixel-equal(off, on)         # non-zero exit on any diff
```

Pass criterion: **byte-for-byte identical** screenshots between flag-off and flag-on for every scene, on both renderers (SC-002). Any difference fails the gate and blocks the change.

## In-process correctness tests

Complementary `Uno.UI.RuntimeTests` (Skia desktop) assert equality vs. a full-frame baseline using `RenderTargetBitmap` + `ImageAssert.AreEqualAsync`/`AreSimilarAsync`. Run via the `/runtime-tests` skill (pass the test class/method). Note: `RenderTargetBitmap` forces the **software** renderer, so it validates record/clip correctness but not the GPU present path — the xvfb script above is what covers the OpenGL/swapchain present.

Scenarios to cover: small update (SC-001), no-op frame skip (SC-004), moved element / old+new repaint (FR-003), overlap + transparency, scrolling, window resize & DPI change (full-frame), theme switch (full-frame parity, SC-005).

## What "done" looks like

- Harness is green and reproducible on software **and** OpenGL.
- All runtime-test scenarios pass equality vs. baseline.
- Diagnostics: `DirtyRectanglesOverlay=true` visibly highlights only the regions being repainted for a small-update scene; the FPS/painted-area instrumentation shows ≥80% painted-area reduction on the ≤5% small-update scene (SC-001) and zero work on no-change frames (SC-004).
- Full-window change (theme switch) is at parity, never slower (SC-005).
