# Internal Contracts: Damage Region Rendering

These are **internal** rendering contracts (no public WinUI API surface). They define the seams between invalidation, the composition present step, and the platform renderers. Signatures are indicative; final names follow surrounding code.

## C1 — Invalidation → damage accumulation

The composition layer MUST contribute changed screen-space bounds to the current frame's damage whenever a visual's appearance changes.

- On `Visual.InvalidatePaint()`, `SetMatrixDirty()`, opacity/visibility/z-order/clip changes: compute the visual's **previous** and **new** total-transform bounds (root/scaled coords) and report both to the owning `CompositionTarget`.
- Contract: `CompositionTarget.AddDamage(SKRect screenBounds)` (thread-safe, under the existing frame gate; no allocation on the hot path).
- Surface resize / DPI change / surface reallocation MUST report full-frame damage.

**Guarantee**: With `EnableDamageRegion == false`, accumulation is inert (or ignored) and the present path behaves exactly as today.

## C2 — Present step (composition)

`CompositionTarget.Draw()` / `SkiaRenderHelper.RenderPicture()` MUST honor a damage region:

- Input: the effective damage for this present (from C3).
- If damage is empty and nothing changed → **skip the present** (no `Clear`, no blit) while leaving the displayed surface correct (FR-005, SC-004).
- If damage is full-frame → behave exactly as today (`Clear` + full `draw_picture`).
- Otherwise → `canvas.ClipRegion(damage)` before `Clear` + `draw_picture`; Skia culls ops outside the clip. Output within the damage MUST be pixel-identical to the full-frame result.

**Guarantee (SC-002)**: For any scene, the union of (preserved previous pixels outside damage) + (repainted pixels inside damage) is pixel-identical to a full-frame repaint.

## C3 — Renderer present + buffer preservation

Each `Uno.UI.Runtime.Skia.*` renderer MUST declare how its surface preserves the previous frame, so the present step can choose a strategy that keeps unchanged pixels correct. Modeled on Avalonia's render-target capability flags (`RetainsPreviousFrameContents`, `IsSuitableForDirectRendering`).

- `RenderTargetCapabilities { RetainsPreviousFrameContents, IsSuitableForDirectRendering, IsCorrupted }` — reported per acquire.
- Strategy selection (per frame):
  - **Persistent-surface renderers** (X11 software `SKBitmap`, FrameBuffer software): retain contents ⇒ present the damage sub-rect directly. X11 software = `XPutImage` with src/dst x/y/w/h of damage; FrameBuffer software = partial `ReadPixels`→`/dev/fb0` copy of damage. No layer.
  - **GPU swapchain renderers** (OpenGL/EGL/Vulkan/DRM/Metal), `!RetainsPreviousFrameContents` (the common case): **primary path** — replay the recorded picture **damage-clipped onto a single Uno-owned persistent offscreen layer** (`SKSurface`/`GRBackendRenderTarget`, always age-1), then blit the layer to the back buffer. No cross-frame damage union, no `EGL_EXT_buffer_age` required.
  - **Optional fast path** — when a backend cheaply guarantees retention (`RetainsPreviousFrameContents && IsSuitableForDirectRendering`, e.g. EGL `EGL_BUFFER_PRESERVED` / single-buffered): render damage-clipped directly to the back buffer and present only the damage sub-rect (no layer).
- `IsCorrupted == true` (device/context lost, swapchain resized) ⇒ recreate target/layer and **full-frame redraw** this frame.

**Guarantee (SC-005, Constitution II)**: Every renderer has a correct strategy or falls back to full-frame present (and full layer blit on GPU) — never producing wrong pixels and never slower than today. The layer adds one cheap full-surface GPU blit per frame while the expensive scene rasterization stays damage-clipped.

## C4 — Configuration & diagnostics

- `FeatureConfiguration.Rendering.EnableDamageRegion` (bool) — master switch (FR-009). Off ⇒ identical to today.
- `FeatureConfiguration.Rendering.DamageRegionOverlay` (bool) — when on, the present step visibly marks the regions it repaints (FR-010); MUST NOT alter the persisted surface content used for correctness comparison beyond the overlay itself (overlay is a debug-only draw atop the presented frame).

## Validation contract (harness)

- `build/test-scripts/run-damage-region-harness.sh <out-dir>` MUST:
  1. Run `SamplesApp.Skia.Generic` under `xvfb-run` capturing `--auto-screenshots` for a fixed scene set, with `EnableDamageRegion=false`, for renderer ∈ {software, OpenGL}.
  2. Repeat with `EnableDamageRegion=true` for the same scenes/renderers.
  3. Assert pixel-equality between the off/on screenshot sets per renderer; non-zero exit on any difference.
- Runtime tests in `Uno.UI.RuntimeTests` MUST cover: small-update (SC-001), no-op frame skip (SC-004), moved element (old+new repaint, FR-003), overlap/transparency, scroll, resize/DPI (full-frame), theme switch (full-frame parity, SC-005) — each asserting equality vs. full-frame baseline.
