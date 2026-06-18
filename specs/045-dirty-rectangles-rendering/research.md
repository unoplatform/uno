# Phase 0 Research: Damage Region Rendering

This document records the decisions that resolve the technical unknowns, grounded in the verified state of the Uno Skia pipeline.

## Current pipeline (verified facts)

- **Two-phase frame**. `CompositionTarget.Render()` (UI thread) records the whole Visual tree into one `SKPicture` via `SkiaRenderHelper.RecordPictureAndReturnPath`, recording into `Visual.InfiniteClipRect`. `CompositionTarget.Draw()` (native render thread) replays the last recorded `SKPicture` onto the window `SKCanvas` via `SkiaRenderHelper.RenderPicture`, which does `canvas.Clear(background)` + `sk_canvas_draw_picture` over the **whole** surface, then `canvas.Flush()`. Files: `CompositionTarget.Rendering.skia.cs`, `SkiaRenderHelper.skia.cs`.
- **Frame hand-off** between threads is already gated (`_frameGate`, `_lastRenderedFrame`) and the FpsHelper already distinguishes recorded-vs-presented generations — useful for "skip no-op frames" accounting.
- **Existing optimizations are recording-side only**: per-visual `_picture`, collapsed `_childrenPicture`, and `EnableVisualSubtreeSkippingOptimization`. None reduce the *present* (the final clear + full-window blit). So the damage-region win is overwhelmingly at present time.
- **Invalidation** already carries the right hooks: `Visual.InvalidatePaint()`, `SetMatrixDirty()`, `InvalidateParentChildrenPicture()`, routed via `Compositor.InvalidateRenderPartial()` → `CompositionTarget.RequestNewFrame()`. These are the natural place to also record the changed visual's screen-space bounds.
- **Renderers present full-window today**: X11 software = `XPutImage` whole bitmap; OpenGL/EGL = `glXSwapBuffers`/`eglSwapBuffers`; Vulkan = `BlitAndPresent`; DRM = page flip; FrameBuffer software = full `ReadPixels`→`/dev/fb0`. None track damage. Renderer selection is via `FeatureConfiguration.Rendering.UseOpenGLOnX11` / `UseVulkanOnX11` and `X11HostBuilder.RenderingBackend(...)`.

## Decision 1 — Where damage regions are computed

**Decision**: Accumulate damage in **screen/root coordinates** at invalidation time. When a visual is invalidated (paint, matrix, opacity, visibility, z-order, clip), union **both** its previous total-transform bounds and its new bounds into a per-`CompositionTarget` damage accumulator. Resize/DPI change/surface realloc force full-frame damage.

**Rationale**: Both old and new bounds are required to erase vacated pixels (FR-003). The invalidation methods already walk the parent chain, and `Visual.skia.cs` already computes total transforms/clip bounds (`GetTotalClipPath`, total matrix), so screen-space bounds are cheap to derive there. Doing it at invalidation (not during record) keeps the accumulator authoritative even when recording is skipped.

**Alternatives considered**: (a) Diff two recorded `SKPicture`s — rejected: expensive and no pixel-diff primitive. (b) Per-visual dirty flags only (today's model) — rejected: marks *which visuals*, not *which screen regions*, so it can't bound the present.

## Decision 2 — How damage regions reduce work at present time

**Decision**: Keep recording the full-tree `SKPicture` (recording is already cache-optimized). At present, `canvas.ClipRegion(damage)` before `Clear`+`draw_picture` so Skia culls ops outside the damage; then present only the damage sub-rectangle to the platform surface. Unchanged pixels come from the preserved previous buffer.

**Rationale**: Clipping the replay is correctness-preserving (same picture, just bounded) and immediately cuts rasterization + blit cost. It avoids a risky rewrite of the recording walk. Recording-side culling can be layered in later as an optimization without changing correctness.

**Alternatives considered**: Recording-side dirty culling first — deferred; higher risk, smaller marginal win than present-side clipping + partial blit.

## Decision 3 — Buffer preservation (the crux)

**Decision**: Branch by surface model, with a **retained offscreen layer** as the primary GPU strategy (Avalonia-style — see "Prior art" below).
- **Persistent-surface renderers** (X11 software `SKBitmap`, FrameBuffer software): previous content is retained, so simply don't clear/copy outside the damage rect — present the damage sub-rect (`XPutImage` with src/dst x/y/w/h; partial `/dev/fb0` copy). Correct by construction. No layer needed.
- **Swapchain/GPU renderers** (OpenGL/EGL/Vulkan/DRM/Metal): the acquired back buffer is N frames old and platform-dependent. **Primary path**: maintain a single Uno-owned persistent offscreen layer (`SKSurface` / `GRBackendRenderTarget`) that always holds the previous frame. Each frame, replay the recorded `SKPicture` onto the layer **clipped to the current frame's damage** (so scene rasterization is bounded), then blit the whole layer to the swapchain back buffer. The layer is always "age 1", so there is **no cross-frame damage union and no per-driver buffer-age plumbing**.
- **Optional fast path**: where a platform *cheaply guarantees* the swapchain retains contents (e.g. EGL with `EGL_BUFFER_PRESERVED`, or a single-buffered/blit-model surface), skip the layer and render damage-clipped **directly** to the back buffer, presenting only the damage sub-rect.
- **Full-frame fallback** on: surface corruption/recreation (device/context lost, resize), DPI change, or `EnableDamageRegion == false`.

**Rationale**: The layer model decouples correctness from driver-specific swapchain semantics: one uniform code path across GL/EGL/Vulkan/DRM/Metal, no `EGL_EXT_buffer_age`/preserved-swap quirks, no need to track damage across the buffer-age window. The cost is one full-surface **GPU texture blit** per frame, which is cheap relative to scene rasterization — the expensive part stays dirty-clipped, so the win is preserved. Full-frame fallback guarantees we are never *worse* than today on any path (Constitution II, SC-005).

**Prior art (Avalonia)**: Avalonia's `ServerCompositionTarget` advertises render-target capability flags (`RetainsPreviousFrameContents`, `IsSuitableForDirectRendering`, `PreviousFrameIsRetained`) and computes `needLayer = !(RetainsPreviousFrameContents && IsSuitableForDirectRendering)`. When the swapchain doesn't retain contents it renders damage regions into a retained intermediate layer and blits it; otherwise it renders directly. Corruption (`IsCorrupted`) forces recreate + full redraw. Khronos notes that `EGL_BUFFER_PRESERVED` "can have severe performance consequences" (a costly per-frame copy-back) and that buffer-age is the efficient alternative — the retained layer dodges both. Refs in plan/answer: Avalonia issues #8527, PR #16849; `Avalonia.Skia/FramebufferRenderTarget.cs`; Khronos EGL Tech Note #1 and `EGL_EXT_buffer_age` spec.

**Alternatives considered**:
- *Buffer-age as the primary GPU strategy* (report `EGL_EXT_buffer_age` / GL preserved-swap / Vulkan image index / DRM BO age and union the last `age` frames' damage, partial-present only) — demoted to the *optional* per-platform fast path. Rejected as primary because it is driver-dependent, requires cross-frame damage bookkeeping, and multiplies correctness risk across five backends for a marginal saving (avoiding one cheap blit).
- *No layer, full-frame fallback everywhere on GPU* — correct but forfeits the GPU win entirely; the layer recovers it at the cost of one blit.

## Decision 4 — Region representation & coalescing

**Decision**: Track damage as a small **capped list of rectangles** unioned into an `SKRegion` for clipping; if the list exceeds a threshold (default ~16) or covers ≳ a large fraction of the surface, collapse to the bounding rect, and past a higher fraction fall back to full-frame. Reuse pooled region/rect storage to avoid per-frame allocations.

**Rationale**: Balances precision (scattered small changes stay cheap) against bookkeeping cost (FR-008, edge case "many small scattered changes"). Thresholds are tunable constants, mirroring the existing `…OptimizationThreshold` pattern.

**Alternatives considered**: Always single bounding rect (simpler, but a top-left + bottom-right change repaints everything between); full `SKRegion` with unbounded rects (precise but costly bookkeeping).

## Decision 5 — Feature flag & diagnostics

**Decision**: Add to `FeatureConfiguration.Rendering` (next to `EnableVisualSubtreeSkippingOptimization`):
- `EnableDamageRegion` (bool) — master switch. **Default OFF** initially; flipped to default-ON for persistent-surface renderers once SC-002 holds across the suite. GPU paths self-fall-back when buffer age is unavailable regardless of the flag.
- `DamageRegionOverlay` (bool) — diagnostic overlay that tints/outlines presented damage regions (FR-010).

**Rationale**: Mirrors existing rendering toggles; provides the FR-009 kill-switch and FR-010 diagnostics; default-off guarantees identical output until each renderer is proven.

## Decision 6 — Validation harness (per user directive)

**Decision**: A `build/test-scripts/run-damage-region-harness.sh` that, modeled on the existing `linux-skia-runtime-tests.sh`, launches `SamplesApp.Skia.Generic` under `xvfb-run --auto-servernum --server-args='-screen 0 1280x1024x24'` with `fluxbox`, using `--auto-screenshots=<dir>` (and `sample=<cat>/<name>` for focused scenes). It runs the same scenes **twice** — once full-frame (flag off) and once damage-region (flag on) — under both **software** (`UseOpenGLOnX11=false`) and **OpenGL** (`UseOpenGLOnX11=true`) renderers, then asserts pixel-equality between the two screenshot sets per renderer. Renderer selection via env/`FeatureConfiguration` or `X11HostBuilder.RenderingBackend`.

In-process correctness uses `Uno.UI.RuntimeTests` with `RenderTargetBitmap` + `ImageAssert.AreEqualAsync`/`AreSimilarAsync` (note: `RenderTargetBitmap.skia.cs` forces the software renderer, so it validates recording/clipping correctness but not the GPU present path — hence the dual-renderer script).

**Rationale**: Directly implements "set up headless testing… alternate software/hardware… visual output identical before and after." The script gives the end-to-end GPU-present coverage that `RenderTargetBitmap` cannot.

**Alternatives considered**: Pure in-process tests only — insufficient, they never exercise the real swapchain present path where buffer-age bugs live.

## Open items deferred to implementation (non-blocking)

- Exact coalescing thresholds (rect count / surface-fraction) — tune against SC-001/SC-005 with the harness.
- macOS Metal & Skia-on-mobile/WASM present paths — full-frame fallback initially; enable per-renderer as buffer-age handling is added.
- Per-renderer default-on timing — gated on SC-002 passing for that renderer in the harness.
