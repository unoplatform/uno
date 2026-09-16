# WASM WebGPU — headless verification (post-merge)

**Date:** 2026-08-12
**Branch:** `feature/drawing-backend-abstraction` (post `feature/breakingchanges` merge)
**Fix under test:** `9ede62cca4` — re-record the visual tree on async renderer switch.

## Question
Does the neutral WebGPU backend render the SamplesApp UI on the WebAssembly head,
after the `feature/breakingchanges` merge? (Pre-merge, render-to-texture on WASM worked.)

## Environment
- Chrome headless (`/usr/bin/google-chrome`), `--enable-unsafe-webgpu --enable-features=Vulkan
  --use-angle=vulkan --use-vulkan=native --enable-unsafe-swiftshader` → Dawn falls back to the
  SwiftShader adapter (no real GPU in CI).
- Xvfb `:99`. Published `SamplesApp.Skia.WebAssembly.Browser` served on `:8099`.
- Env (baked in `uno-config.js`): `UNO_WEBGPU=1`, `UNO_WEBGPU_TRACE=1`, `UNO_WEBGPU_READBACK=1`.
- Harness: `/tmp/wgpu-test/verify2.js` (Playwright-core); full console at
  `wasm-webgpu-readback-console.log`.

## Why the offscreen readback, not the canvas
SwiftShader (Dawn's headless fallback) renders WebGPU correctly **to a texture** but does not
composite the HTML canvas backbuffer in a way Playwright can screenshot; a JS
`getCurrentTexture()` readback also races the app's own per-frame present. So the canvas capture
is unreliable **by environment**, not by defect. The backend's built-in `UNO_WEBGPU_READBACK=1`
copies the real offscreen resolve target (`_presentTex`, the exact texture the tree renders into)
to CPU off the JS event loop and logs its pixel stats — the reliable headless proof.

## Result — PASS
The app boots (1414 samples), imports the WebGPU device from JS, and the backend initializes:

```
[webgpu] backend init — pipeline=True msaa=4x scale=1 colorFormat=RGBA8Unorm
[webgpu] TEX #2 Surface.msaa 1024x768 x4
[webgpu] TEX #3 Surface.depth 1024x768 x4
[webgpu] TEX #4 ImageTexture.upload 64x64 x1        <- a real image is uploaded + drawn
```

Offscreen render-target readback across five presents:

```
opaquePixels=786432 (of 786432)   lumMin=26  lumMax=253
opaquePixels=740000 (of 786432)   lumMin=26  lumMax=255   (mid-resize, 1000x740)
opaquePixels=786432 (of 786432)   lumMin=25  lumMax=253
opaquePixels=786432 (of 786432)   lumMin=26  lumMax=253
opaquePixels=786432 (of 786432)   lumMin=26  lumMax=253
```

- **Fully painted:** every pixel opaque (no transparent gaps → the tree walk emits draws; a blank
  frame would be cleared-transparent = 0 opaque).
- **Real UI content, not a flat clear:** luminance spans 26..253 (dark text/chrome + bright
  surfaces). A flat background would give `lumMin == lumMax`.
- **Live, not a stuck frame:** the opaque count changes with the viewport (740000 during the
  resize to 1000x740), and an image texture is uploaded and drawn.
- No page errors; SkiaSharp managed/native versions aligned (4.151.1).

## Conclusion
WASM WebGPU renders the full SamplesApp UI to the offscreen texture — the pre-merge capability is
intact post-merge, and the async renderer-switch fix (`9ede62cca4`) makes the Skia→WebGPU handoff
re-record cleanly. Visible on-canvas compositing is a standard render-pass blit from this
proven-good offscreen texture; it cannot be verified under headless SwiftShader and needs a
real-GPU browser to confirm on screen.
