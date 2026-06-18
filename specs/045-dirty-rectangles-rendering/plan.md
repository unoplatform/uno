# Implementation Plan: Damage Region Rendering

**Branch**: `045-damage-region-rendering` | **Date**: 2026-06-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/045-damage-region-rendering/spec.md`

## Summary

Today the Uno Skia pipeline renders in two phases per frame: **Record** (`CompositionTarget.Render()` walks the Visual tree into a single `SKPicture` via `SkiaRenderHelper.RecordPictureAndReturnPath`, clipped to `InfiniteClipRect`) and **Present** (`CompositionTarget.Draw()` → `SkiaRenderHelper.RenderPicture()` calls `canvas.Clear(...)` then `sk_canvas_draw_picture(...)` over the **entire** window surface). Per-visual `SKPicture` caching and subtree-collapsing already reduce *recording* cost, but the final present always clears and repaints the whole surface.

This feature adds **damage-region rendering**: accumulate, per frame, the union of screen-space regions whose visual output changed since the last presented frame; at present time, clip the canvas to that region (so Skia culls draw ops and only the changed pixels are repainted), preserve unchanged pixels from the previous frame, and present only the affected sub-region to the platform surface. The hard constraint is **buffer preservation**: software renderers retain a persistent backing bitmap (trivially correct), but double/triple-buffered GPU swapchains (OpenGL/EGL/Vulkan/DRM/Metal) hand back a buffer that is N frames old. Rather than depend on driver-specific swapchain semantics, GPU renderers render the damage-clipped scene onto a single Uno-owned persistent offscreen **layer** (always holds the previous frame) and blit that layer to the back buffer each frame — the Avalonia approach (see research.md Decision 3) — with a full-frame fallback on surface corruption/resize.

**Approach** (per the user's directive): first stand up a **headless xvfb + SamplesApp.Skia.Generic harness** that can run under both the software and OpenGL X11 renderers and capture screenshots; lock in a before/after **pixel-equality** validation gate; only then iterate on the implementation behind a `FeatureConfiguration.Rendering` flag, proving at each step that output stays pixel-identical to the full-frame baseline.

## Technical Context

**Language/Version**: C# (net9.0/net10.0 multi-target); rendering code is `.skia.cs` / `.crossruntime.cs`
**Primary Dependencies**: SkiaSharp (`SKPicture`, `SKCanvas`, `SKRegion`, `SKRect`, `SKSurface`, `GRContext`); Uno.UI.Composition; X11/EGL/GLX/Vulkan/DRM interop in `Uno.UI.Runtime.Skia.*`
**Storage**: N/A (in-memory frame state only)
**Testing**: `Uno.UI.RuntimeTests` (Skia desktop, headless via xvfb) using `RenderTargetBitmap` + `ImageAssert.AreEqualAsync` / `AreSimilarAsync`; new harness driving `SamplesApp.Skia.Generic` under `xvfb-run`
**Target Platform**: Skia rendering pipeline — Desktop X11 (software + OpenGL/EGL/Vulkan), Linux FrameBuffer (software + DRM/KMS), Win32, macOS (Metal), Skia-on-Android/iOS/WASM. Native (non-Skia) UI targets are maintenance-only and must remain unaffected.
**Project Type**: Cross-platform UI framework (single repo, platform-suffixed source)
**Performance Goals**: For a ≤5% small-update scene, reduce per-frame painted area ≥80% (SC-001) and per-frame render cost vs. full repaint (SC-003); zero draw work on no-change frames (SC-004); full-window change at parity, never slower (SC-005); 60fps targets preserved.
**Constraints**: Output MUST be pixel-identical to full-frame repaint across all change patterns (SC-002); no public API/behavior change beyond performance (FR-012); no regression in existing runtime tests (SC-007); per-frame code paths must avoid new allocations (Constitution IV).
**Scale/Scope**: Touches the Skia composition present path (`CompositionTarget`, `SkiaRenderHelper`, `Visual.skia.cs` invalidation) and each `Uno.UI.Runtime.Skia.*` renderer's present/`Flush` path. ~6-10 source files + new runtime tests + new headless harness.

### Key code touch-points (verified)

- `src/Uno.UI/UI/Xaml/Media/CompositionTarget.Rendering.skia.cs` — `Render()` (record), `Draw()` (present). Present clip + partial-present orchestration lives here.
- `src/Uno.UI/Helpers/SkiaRenderHelper.skia.cs` — `RecordPictureAndReturnPath()`, `RenderPicture()` (the `canvas.Clear()` + `draw_picture` that must become dirty-clipped).
- `src/Uno.UI.Composition/Composition/Visual.skia.cs` — `InvalidatePaint()`, `SetMatrixDirty()`, `InvalidateParentChildrenPicture()` (where per-visual old/new screen-space bounds get accumulated); existing `VisualFlags` (`PaintDirty`, `MatrixDirty`, `ChildrenSKPictureInvalid`).
- `src/Uno.UI.Composition/Composition/Compositor.skia.cs` — `InvalidateRenderPartial()`, `RenderRootVisual()`.
- `src/Uno.UI/FeatureConfiguration.cs` — `FeatureConfiguration.Rendering` (line ~961) is where `EnableDamageRegion` (+ a diagnostic-overlay flag) belong, next to `EnableVisualSubtreeSkippingOptimization`.
- Renderer present paths: `Uno.UI.Runtime.Skia.X11/Rendering/{X11SoftwareRenderer,X11OpenGLRenderer,X11EGLRenderer,X11VulkanRenderer}.cs`, `Uno.UI.Runtime.Skia.Linux.FrameBuffer/Rendering/{SoftwareRenderer,DRMRenderer}.cs`, base `X11Renderer.cs` / `FrameBufferRenderer.cs`. These need a damage-region-aware present; GPU renderers additionally need a retained offscreen layer + blit and capability/corruption reporting (software renderers present the damage sub-rect directly from their persistent bitmap).
- `src/SamplesApp/SamplesApp.Skia.Generic/` + `src/SamplesApp/SamplesApp.Shared/App.Tests.cs` (`--auto-screenshots`, `sample=<cat>/<name>` CLI args) — driven by the new xvfb harness.

### NEEDS CLARIFICATION

None blocking. Two decisions are intentionally deferred to implementation/rollout and tracked in research.md: (a) default-on vs opt-in per renderer class, resolved as "default-on for software/persistent-surface renderers once validated; GPU enabled via the retained layer, full-frame fallback on corruption/resize"; (b) region representation & coalescing thresholds (single bounding rect vs `SKRegion` vs capped rect list), resolved in research.md.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. WinUI API Fidelity** — PASS. Internal rendering optimization only; no public WinUI API changes. The opt-out/diagnostic switches are Uno-specific `FeatureConfiguration` (allowed; not a WinUI API surface).
- **II. Cross-Platform Parity** — PASS (with discipline). Targets the Skia pipeline (all Skia platforms). Each renderer either gets a correct dirty-aware present or **falls back to full-frame** (current behavior) — so parity/output is never worse anywhere, and native (non-Skia) UI is untouched. Platform-specific present code stays in the respective `.Runtime.Skia.*` renderer files.
- **III. Test-First Quality Gates** — PASS. Plan front-loads a failing-first pixel-equality harness + `Uno.UI.RuntimeTests` cases (red → implement → green). This is the central validation per the user's directive.
- **IV. Performance & Resource Discipline** — PASS (this is the point). Must demonstrate before/after painted-area + cost reduction and **zero** new per-frame allocations on the hot path (reuse pooled `SKRegion`/rect buffers); full-window change must stay at parity.
- **V. Generated Code Boundaries** — PASS. No `Generated/` edits.
- **VI. Backward Compatibility** — PASS. Behind a flag; default preserves identical visual output. No `feat!`.
- **VII. WinUI Implementation Alignment** — N/A/RECOMMENDED. WinUI's compositor does internal damage tracking but the Skia present path is Uno-specific; no C++ port required. Consult conceptually where useful.

**Result**: No violations. Complexity Tracking table omitted.

## Project Structure

### Documentation (this feature)

```text
specs/045-damage-region-rendering/
├── plan.md              # This file
├── research.md          # Phase 0 output — approach decisions, retained-layer GPU strategy, harness
├── data-model.md        # Phase 1 output — DamageRegion / Invalidation / FrameDamage entities
├── quickstart.md        # Phase 1 output — xvfb harness + before/after pixel-equality run guide
├── contracts/
│   └── damage-region-rendering.md   # Internal contracts: invalidation→damage, present, retained layer, flags
├── checklists/
│   └── requirements.md  # Spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
src/
├── Uno.UI.Composition/Composition/
│   ├── Visual.skia.cs                 # accumulate old+new screen-space bounds on invalidation
│   └── Compositor.skia.cs             # route invalidation → CompositionTarget damage accumulator
├── Uno.UI/
│   ├── UI/Xaml/Media/
│   │   ├── CompositionTarget.Rendering.skia.cs        # damage accumulation + dirty-clipped Draw()
│   │   └── CompositionTarget.RenderScheduling.skia.cs # thread-safe per-frame damage gate; skip no-op frames
│   ├── Helpers/SkiaRenderHelper.skia.cs               # dirty-clipped RenderPicture / partial present helpers
│   └── FeatureConfiguration.cs                        # Rendering.EnableDamageRegion + diagnostics flag
├── Uno.UI.Runtime.Skia/Hosting/                       # shared renderer base: damage-aware present + retained layer/caps
├── Uno.UI.Runtime.Skia.X11/Rendering/                 # software (XPutImage sub-rect) + GL/EGL/Vulkan present
├── Uno.UI.Runtime.Skia.Linux.FrameBuffer/Rendering/   # software fb sub-copy + DRM retained layer/blit
└── SamplesApp/SamplesApp.Skia.Generic/                # target app for the headless harness

src/Uno.UI.RuntimeTests/Tests/Windows_UI_Composition/  # new damage-region pixel-equality + no-op-frame tests

build/test-scripts/                                     # new: run-damage-region-harness.sh (xvfb, sw + gl)
```

**Structure Decision**: Existing Uno multi-project layout; no new projects. Shared renderer behavior (damage-aware present + retained-layer/capability reporting) goes into the existing `Uno.UI.Runtime.Skia` shared host base so each platform renderer overrides only its present/`Flush`. Feature flag lives in the existing `FeatureConfiguration.Rendering` class. Tests extend `Uno.UI.RuntimeTests`; the headless harness is a shell script under `build/test-scripts/` modeled on the existing `linux-skia-runtime-tests.sh` xvfb invocation.

## Phased rollout (implementation strategy)

1. **Harness first (P-validation)** — headless xvfb run of `SamplesApp.Skia.Generic` under both software and OpenGL X11 renderers; capture screenshots; establish a baseline-vs-changed pixel-equality gate. Nothing ships until this is green and reproducible. (quickstart.md)
2. **Damage tracking (no behavior change)** — accumulate per-frame screen-space damage regions from invalidation (old+new bounds) into a thread-safe per-`CompositionTarget` accumulator; add `FeatureConfiguration.Rendering.EnableDamageRegion` (default OFF) and a diagnostic-overlay flag. With the flag off, present stays full-frame — output provably unchanged.
3. **Software present path** — when enabled, clip `Draw()`/`RenderPicture()` to the damage region, skip no-change frames, and present only the damage sub-rect (X11 `XPutImage` sub-rect; FrameBuffer partial copy). Validate pixel-equality on software renderer. Persistent backing bitmap makes preservation trivially correct.
4. **GPU present path — retained layer** (Avalonia-style, see research.md Decision 3) — add a single Uno-owned persistent offscreen layer (`SKSurface`/`GRBackendRenderTarget`); replay the picture damage-clipped onto the layer, then blit the layer to the swapchain. One uniform path across GL/EGL/Vulkan/DRM/Metal — no per-driver buffer-age plumbing, no cross-frame damage union. Recreate layer + full-frame on corruption/resize. Validate pixel-equality on OpenGL/EGL, then Vulkan/DRM. (Optional later: per-backend direct-to-swapchain fast path where retention is cheaply guaranteed.)
5. **Edge-case hardening + perf** — overlap/transparency/transform/scroll/resize/DPI/theme-switch correctness; coalescing + full-repaint threshold; allocation audit; default-on for software once SC-002 holds across the suite.

## Complexity Tracking

> No constitution violations — table intentionally empty.
