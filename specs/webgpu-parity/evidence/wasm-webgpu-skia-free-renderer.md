# WASM WebGPU — Skia-free renderer via per-seam fallback

**Date:** 2026-08-12
**Commits:** `02ef2e6b2d` (per-seam fallback), `09a6270f4b` (skip-frame guard), plus the
WASM head registering `ManagedDrawingFactory` as its base factory (LOCAL head setup).

## What changed
The implicit "light up SkiaSharp if present" fallback used to run the whole
`SkiaBackend.Register()` on first touch of ANY empty seam — so accessing the font seam also
installed the Skia renderer and clobbered a declared graphics backend. That is why SkiaSharp was
the renderer on the WASM WebGPU head and painted the first frames.

Now the fallback is **per-seam**:
- **font provider** / **image decoder** — render-independent content seams; each falls back to its
  own Skia impl only if that seam is empty;
- **graphics backend** (matched drawing-factory + renderer pair) — falls back to Skia only if NO
  backend was declared via `GraphicsRegistry.Register`.

The WASM WebGPU head now registers its base drawing factory explicitly (like the desktop head):
`DrawingFactory.Register(new ManagedDrawingFactory())` — Skia-free geometry. The WebGPU provider
decorates it (shader/texture/offscreen are the renderer's own; geometry delegates to the base).
`CompositionTarget.Render()` skips a frame while no renderer is installed yet (async device import),
instead of forcing the throwing `Renderer` getter.

Net result on the WASM WebGPU head:
- **renderer:** WebGPU only — **no Skia renderer registered, no Skia-painted frames**;
- **geometry:** managed (`ManagedDrawingFactory`) — SkiaSharp-free;
- **font shaping + image decode:** Skia impls via independent per-seam fallback (managed font engine
  is metrics-only, so Skia still shapes text for now — cleanly isolated to those two seams).

## Build requirement (was lost across context compaction — cost several cycles)
The WASM WebGPU native (`wgpu-wasm.targets` → Dawn/emdawnwebgpu) is imported ONLY under
`-p:UnoWebGpuWasm=true`. Without it, `wgpuCreateInstance` throws `DllNotFoundException` and WebGPU
never initializes (silently — the fire-and-forget init's error log did not surface). Publish with:

```
dotnet publish src/SamplesApp/SamplesApp.Skia.WebAssembly.Browser/... \
  -c Release -f net10.0 -p:UnoWebGpuWasm=true -p:UnoFastDevBuild=true \
  -p:UnoTargetFrameworkOverride=net10.0
```

## Runtime proof (Chrome headless / SwiftShader, `UNO_WEBGPU_READBACK=1`)
Init sequence (no exceptions): instance → JS device import (`devPtr!=0`) → `backend init` →
pipelines built → `InitializeAsync returned` → `CreateGraphics` did NOT throw (base factory present).

Trace shows real rendering: `TEX ImageTexture.upload 64x64` (image drawn), `TEX Pool.Rent 512x384`
(offscreen layers). Offscreen render-target readback across five presents:

```
opaquePixels=786432 (of 786432)   lumMin=26 lumMax=250..255
opaquePixels=740000 (of 786432)   (mid-resize, 1000x740)
opaquePixels=786432 (of 786432)   (x3)
```

Fully painted, wide luminance range (real UI, not a flat clear), varying per frame — identical
quality to the earlier Skia-backed run, now with WebGPU as the sole renderer and Skia-free geometry.
Visible on-canvas compositing remains unprovable under headless SwiftShader (needs a real-GPU browser).
Full console: `wasm-webgpu-skia-free-renderer-console.txt`.
