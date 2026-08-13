# WebGPU perf-parity audit — reference (`ramez/webgpu-experiment`) vs neutral port

**Date:** 2026-08-13. **Method:** 5 parallel subagents, each diffing *work done* (not pixels) across a
subsystem. **Baseline:** reference runs its best-perf combo `ARENA=1 SLAB=1 CACHE=1 DIRTY=1 COALESCE=1 MSAA=2
GLYPHATLAS=1 NOTEXT=0` (RUNNING-CONTEXT §3). Env-config knobs themselves (present mode, MSAA count, pipeline poll)
excluded as the *answer*, but the capabilities the combo enables are in scope. Rule: neutral may have MORE, not LESS.

**Why the earlier pixel-diff parity harness (tasks #10, #23) missed all of this:** it verified *correctness* parity
(same command stream / same pixels). A full-frame blur, a stencil-fan glyph, a re-uploaded buffer all produce the
*correct* image — just slower. Work-done divergences were invisible to a pixel diff.

## Real gaps — neutral does LESS (ranked by impact)

1. **Acrylic backdrop blur is full-frame, not region-limited.** THE acrylic regression (independently found by 2
   agents). Neutral: `BlurPyramid(target.View, _s.Width, _s.Height, …)` (`WebGpuBackend.cs:3778`, `:3574`) blurs the
   whole framebuffer per backdrop, scissoring only the composite. Reference: extracts the element AABB padded by
   `sigma+8` and blurs only that sub-rect (`WebGpuDrawList.cs:1982-2001`). ~10–15× overdraw for a flyout/menu; scales
   with window size, not element size. Fix: region-extract + region-uv composite.
2. **Blur pyramid depth + kernel.** Compounds #1; also affects shadows. Neutral: one ½ downsample + a gaussian whose
   tap count = `ceil(sigma*3)` (~91 taps at large sigma) over a quarter-frame texture (`:2853-2887`, `:760-768`).
   Reference: sigma-scaled downsample pyramid + fixed 9-tap on a tiny top texture (`WebGpuDrawList.cs:1993`,
   `:1139-1146`). Neutral cost grows linearly with blur radius; reference is near-constant. Fix: sigma-scaled pyramid
   + fixed small kernel.
3. **No GPU glyph atlas.** Part of the reference's baseline (`GLYPHATLAS=1`). Reference rasterizes each glyph once
   into a coverage atlas → batched textured quads, no MSAA (`GlyphAtlas.cs`, `WebGpuDrawList.cs:583`). Neutral has no
   atlas: every glyph run → `BuildGlyphRunOutline` → stencil-fan + cover every paint, forcing an MSAA pass for all
   text and full CPU+GPU rebuild for dynamic/scrolling text (`ParsedText.cs:890`, `WebGpuBackend.cs:2132`, `:3023`).
   Big for text-heavy/scrolling UI. CAVEAT: reference atlas is SkiaSharp-based (`font is SKFont`); neutral needs a
   backend-neutral coverage-atlas rasterizer (managed/HarfBuzz-scale) — real gap, large effort.
4. **Frame-solid solids/rrects re-extract + re-upload on transform change (scroll).** The known "solid-scroll" TODO.
   Path fills already use device verts + transform table (cheap re-stamp); solids/rrects in the frame-solid path do
   not — a scrolling list re-tessellates + re-uploads every visible solid every frame (`WebGpuBackend.cs:3288-3357`;
   frame-solid takes precedence over the arena re-stamp at `:3278`). Fix: apply the neutral's own device-space +
   transform-table technique to solids/rrects.
5. **No skip-identical / partial vertex upload (the `DIRTY` capability).** Neutral always full-uploads immediate and
   changed buffers (`MakeBuffer` `:2685`, `WebGpuSlab.Put` writes the whole slice `:1833-1858`). Reference keeps a CPU
   shadow, `CommonPrefixLength`-diffs, skips identical, uploads only `[lo..hi]` (`Rendering.cs:601-612`). Bounded
   (static content already resident), but on immediate/dynamic buffers it's redundant `QueueWriteBuffer` traffic. Fix:
   per-key CPU shadow + prefix/suffix diff in `Put`/`MakeBuffer`.
6. **On-window MSAA target `StoreOp.Store` every frame.** Universal small per-frame tax at 100–200% DPI. Reference's
   no-effects fast path uses `StoreOp.Discard` (resolve on-tile, skip writing all N samples) (`Rendering.cs:242`);
   neutral's dedicated (non-pooled) target always Stores (`WebGpuBackend.cs:3639`) so a `load:true` overlay pass could
   reload it. Fix: thread a "will be reloaded" flag; Discard when no overlay pass follows.
7. **Xform storage bind-group recreated every frame.** Modest. Reference caches it, rebuilding only on buffer growth
   (`Owned.cs:710`, persistent StorageBuffer); neutral rents the table from `BufferPool` (pointer changes) and
   `CreateBindGroup` every frame with path fills (`WebGpuBackend.cs:3614-3623`). Fix: persistent xform buffer +
   identity-keyed bind-group cache.
8. **Opaque short-circuit misses `sigma<=0`.** Minor. Reference short-circuits on `isOpaque || blurSigma<=0`
   (`WebGpuDrawList.cs:725`); neutral only on `fx.Color.A==255` (`WebGpuBackend.cs:2318`).

## NOT gaps (hypotheses tested and rejected)

- **Whole-frame content-hash skip:** reference **removed** it deliberately (`WebGpuDrawList.cs:1284`) — the per-frame
  vertex hash cost more than it saved. Parity. (Corrects an earlier interim claim that this was the difference.)
- **Render bundles / ExecuteBundles:** reference **removed** them (`:1499`, ~6× slower on wgpu-native/Intel with
  per-widget scissor/stencil changes). Parity; adding them would regress.
- **Full-window `LoadOp.Load` pass-reopen for backdrops:** AT PARITY — both branches do it (ref `:2021`/`:1868`,
  neutral `:3783`). Not the acrylic cause.
- **Coalescing, scissor dedup, in-pass clip depth-mask, transform re-stamp (path fills), per-frame present readback
  (none on either), pipeline/sampler/texture/buffer/image/clip/gradient caches:** all AT PARITY or neutral ahead.
- **Shadow region-limiting:** neutral is AHEAD — it bounds shadow coverage to the silhouette bbox; the reference
  blurs shadows full-frame.
- **Layer/shadow/mask offscreen bounds-sizing (task #14):** a real optimization but full-size on BOTH — not a
  reference-vs-neutral gap.

## Bottom line
The acrylic slowdown = gaps **1 + 2** (region-blur + pyramid). The other live gaps — **3** (glyph atlas), **4**
(solid-scroll), **5** (dirty-upload), **6** (MSAA store), **7** (xform bind-group) — are independent perf-parity
items. Everything else is at parity or neutral is ahead. Two of my own earlier hypotheses (frame-skip, render
bundles) were reference-removed dead ends.

## Resolution (2026-08-13)

- **Gap 1 (region-limited blur)** — DONE. `BlurPyramidRegion` extracts the element AABB padded by `sigma+8` and both
  backdrop paths (pooled + case-6 segmented) blur and composite only that sub-rect. Confirmed at runtime: the
  acrylic sample's blur pool textures are `223×270 → 111 → 55 → 27`, not the 1024×768 framebuffer.
- **Gap 2 (pyramid depth + kernel)** — DONE. Sigma-scaled downsample depth (`levels = clamp(round(log2(sigma/2)),1,5)`)
  + a fixed normalized 9-tap gaussian in `BlurWgsl` (downsample branch via `ctrl.x`, region remap via `srcOrigin`/
  `srcScale`). Cost is now near-constant in blur radius.
- **Gap 5 (skip-identical / dirty upload)** — DONE. `WebGpuSlab.Put` diffs each slice against the CPU shadow: skips
  the `QueueWriteBuffer` entirely when byte-identical (static UI) and uploads only the changed `[lo..hi]` sub-range
  otherwise. (Immediate one-shot buffers via `MakeBuffer` stay full-upload — no stable key to diff against, same as
  the reference, whose dirty path is keyed persistent VBs only.)
- **Gap 6 (on-window MSAA StoreOp.Store)** — DONE, at the root. The immediate-mode overlay (FPS/diagnostics, host
  cursor) is now inlined into the frame's command list and rendered in ONE pass at `Dispose` (present render deferred
  from `Replay`), mirroring the reference's inlined FPS image. With no follow-up `LoadOp.Load` overlay pass, the
  fast path (`backdrops.Count == 0`) resolves its MSAA on-tile (`StoreOp.Discard`); only a case-6 backdrop segment
  (which reopens with `LoadOp.Load`) keeps `StoreOp.Store`.
- **Gap 7 (xform bind-group recreated every frame)** — DONE. `WebGpuDevice.EnsureXformBindGroup` keeps a persistent
  storage buffer (grown 1.5×) + a bind group cached by buffer identity, rebuilt only on growth. Only the main pass
  uses it; nested/pooled passes still rent transient buffers so their distinct tables never alias it within a frame.
  HARDENING (2026-08-13): the outgrown buffer + bind group are now released via the DEFERRED path (`_pendingBuffers`/
  `_pendingBindGroups`, freed at next frame start), not immediately — an immediate release could reclaim a resource
  the prior in-flight frame's submitted commands still bind under pipelining.
- **Gap 8 (opaque short-circuit)** — DONE. `DrawEffectBackdrop` now short-circuits on `Color.A == 255 || (sigma<=0)`.
- **Gap 4 (solid-scroll)** — ATTEMPTED then REVERTED. The local-space frame-solid restamp (`EmitFrameSolidLocal`/
  `StampFrameClip`) caused a scroll-triggered vertex-buffer use-after-free crash on real HW (`wgpu-core storage.rs:
  Buffer[Id] does not exist` at `SetVertexBuffer`). A proven secondary bug: on scroll-out→in, the hit branch only
  `MarkLive`d the slab slice and reused cached `FrameOrder` byte offsets that a culled-then-reclaimed slice can alias
  (wrong-render, in-bounds because the slab only grows). Reverted (commit `cb4611e9cb`) to restore the proven
  device-space frame-solid path, which self-heals on scroll (transform change → rebuild + re-`Put`). Still open; a
  correct redo needs per-vertex transform-table slots for solids/rrects with careful buffer-lifetime handling.
- **Gap 3 (glyph atlas)** — deferred to discussion (needs a backend-neutral coverage-atlas rasterizer).

Runtime validation: desktop X11 head on lavapipe (`Xvfb :99`), MSAA on, both the acrylic/segmented path
(`Brushes/BasicAcrylicBrushTest`) and the fast path (`Diag/WebGpuDiag`: solids, rrect border, text, gradients),
plus the FPS overlay forced on to exercise the inline-overlay concat — all render with no wgpu/validation errors.
